using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Constants;
using ECommerce.Infrastructure.Identity;
using FluentValidation;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Infrastructure.Services;

/// <summary>
/// Customer-facing authentication service.
/// Handles registration, email/password login, Google OAuth login, and profile retrieval.
/// Uses ASP.NET Core Identity for all user management concerns.
/// </summary>
internal sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser>    _userManager;
    private readonly SignInManager<ApplicationUser>  _signInManager;
    private readonly IJwtProvider                    _jwtProvider;
    private readonly IValidator<RegisterRequest>     _registerValidator;
    private readonly IValidator<LoginRequest>        _loginValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequest>  _resetPasswordValidator;
    private readonly IEmailService                   _emailService;
    private readonly GoogleSettings                  _googleSettings;
    private readonly Authentication.JwtSettings      _jwtSettings;
    private readonly ApplicationDbContext            _dbContext;
    private readonly ILogger<AuthService>            _logger;

    public AuthService(
        UserManager<ApplicationUser>    userManager,
        SignInManager<ApplicationUser>  signInManager,
        IJwtProvider                    jwtProvider,
        IValidator<RegisterRequest>     registerValidator,
        IValidator<LoginRequest>        loginValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ResetPasswordRequest>  resetPasswordValidator,
        IEmailService                   emailService,
        IOptions<GoogleSettings>        googleOptions,
        IOptions<Authentication.JwtSettings> jwtOptions,
        ApplicationDbContext            dbContext,
        ILogger<AuthService>            logger)
    {
        _userManager       = userManager;
        _signInManager     = signInManager;
        _jwtProvider       = jwtProvider;
        _registerValidator = registerValidator;
        _loginValidator    = loginValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator  = resetPasswordValidator;
        _emailService      = emailService;
        _googleSettings    = googleOptions.Value;
        _jwtSettings       = jwtOptions.Value;
        _dbContext         = dbContext;
        _logger            = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        // ── Structural validation ──────────────────────────────────────────────
        var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<AuthResponse>(Error.Validation(
                "VALIDATION_ERROR",
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        // ── Email uniqueness ───────────────────────────────────────────────────
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return Result.Failure<AuthResponse>(Error.Conflict(
                "EMAIL_ALREADY_EXISTS",
                $"The email '{request.Email}' is already registered."));

        // ── Create Identity user ───────────────────────────────────────────────
        var user = new ApplicationUser
        {
            UserName  = request.Email,
            Email     = request.Email,
            FirstName = request.FirstName,
            LastName  = request.LastName,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            _logger.LogWarning("Registration failed for {Email}: {Errors}", request.Email, errors);
            return Result.Failure<AuthResponse>(Error.Validation("REGISTRATION_FAILED", errors));
        }

        // ── Assign Customer role ───────────────────────────────────────────────
        await _userManager.AddToRoleAsync(user, Roles.Customer);

        _logger.LogInformation("Customer registered: {UserId} ({Email})", user.Id, user.Email);

        return Result.Success(await BuildAuthResponseAsync(user));
    }

    /// <inheritdoc/>
    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        // ── Structural validation ──────────────────────────────────────────────
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<AuthResponse>(Error.Validation(
                "VALIDATION_ERROR",
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        // ── Find user ──────────────────────────────────────────────────────────
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Result.Failure<AuthResponse>(Error.Validation(
                "INVALID_CREDENTIALS", "Invalid email or password."));

        // ── Verify the user is a Customer (not an Admin trying to use this endpoint)
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(Roles.Customer))
            return Result.Failure<AuthResponse>(Error.Validation(
                "INVALID_CREDENTIALS", "Invalid email or password."));

        // ── Check password ─────────────────────────────────────────────────────
        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user, request.Password, lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            if (signInResult.IsLockedOut)
                return Result.Failure<AuthResponse>(Error.Validation(
                    "ACCOUNT_LOCKED_OUT", "Account is temporarily locked due to too many failed attempts."));

            return Result.Failure<AuthResponse>(Error.Validation(
                "INVALID_CREDENTIALS", "Invalid email or password."));
        }

        _logger.LogInformation("Customer logged in: {UserId} ({Email})", user.Id, user.Email);

        return Result.Success(await BuildAuthResponseAsync(user));
    }

    /// <inheritdoc/>
    public async Task<Result<AuthResponse>> GoogleLoginAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
            return Result.Failure<AuthResponse>(Error.Validation(
                "INVALID_GOOGLE_TOKEN", "Google ID token is required."));

        // ── Verify Google ID token server-side ─────────────────────────────────
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_googleSettings.ClientId]
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, validationSettings);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("Google token validation failed: {Message}", ex.Message);
            return Result.Failure<AuthResponse>(Error.Validation(
                "INVALID_GOOGLE_TOKEN", "The provided Google token is invalid or has expired."));
        }

        var email = payload.Email;
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<AuthResponse>(Error.Validation(
                "INVALID_GOOGLE_TOKEN", "Google account does not have an email address."));

        // ── Find-or-create user ────────────────────────────────────────────────
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            // New customer — auto-register
            user = new ApplicationUser
            {
                UserName  = email,
                Email     = email,
                FirstName = payload.GivenName ?? string.Empty,
                LastName  = payload.FamilyName ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true   // Google already verified the email
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                _logger.LogError("Google auto-registration failed for {Email}: {Errors}", email, errors);
                return Result.Failure<AuthResponse>(Error.Validation("REGISTRATION_FAILED", errors));
            }

            await _userManager.AddToRoleAsync(user, Roles.Customer);
            _logger.LogInformation("New customer auto-registered via Google: {UserId} ({Email})", user.Id, email);
        }
        else
        {
            // ── Ensure the existing user is a Customer ─────────────────────────
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(Roles.Customer))
                return Result.Failure<AuthResponse>(Error.Validation(
                    "INVALID_CREDENTIALS",
                    "This email is associated with an admin account. Google login is not available for administrators."));

            _logger.LogInformation("Existing customer signed in via Google: {UserId} ({Email})", user.Id, email);
        }

        // ── Link Google login provider (idempotent) ────────────────────────────
        var logins  = await _userManager.GetLoginsAsync(user);
        var already = logins.Any(l => l.LoginProvider == "Google" && l.ProviderKey == payload.Subject);
        if (!already)
        {
            await _userManager.AddLoginAsync(
                user,
                new UserLoginInfo("Google", payload.Subject, "Google"));
        }

        return Result.Success(await BuildAuthResponseAsync(user));
    }

    /// <inheritdoc/>
    public async Task<Result<UserProfileResponse>> GetProfileAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<UserProfileResponse>(Error.NotFound(
                "USER_NOT_FOUND", $"User with ID '{userId}' was not found."));

        var roles = await _userManager.GetRolesAsync(user);

        return Result.Success(new UserProfileResponse
        {
            UserId      = user.Id,
            Email       = user.Email ?? string.Empty,
            FirstName   = user.FirstName ?? string.Empty,
            LastName    = user.LastName  ?? string.Empty,
            FullName    = user.DisplayName,
            Role        = roles.FirstOrDefault() ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            CreatedAt   = user.CreatedAt
        });
    }

    public async Task<Result> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _forgotPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(Error.Validation(
                "VALIDATION_ERROR",
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            // Do not leak that the user does not exist.
            _logger.LogInformation("Password reset requested for {Email}, but email not found.", request.Email);
            return Result.Success();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var resetLink = $"https://your-frontend-domain.com/reset-password?token={encodedToken}&email={request.Email}";
        
        var htmlBody = $@"
<div dir=""rtl"" style=""font-family: 'Tajawal', 'Segoe UI', Tahoma, Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #f9f9f9; padding: 20px; border-radius: 8px; border: 1px solid #e0e0e0;"">
    <div style=""text-align: center; margin-bottom: 20px;"">
        <h1 style=""color: #30584b; margin: 0; font-size: 28px;"">شيرين للسجاد</h1>
        <p style=""color: #666; margin-top: 5px; font-size: 14px;"">أرقى تشكيلة من شراشف وسجاد الصلاة</p>
    </div>
    <div style=""background-color: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.05);"">
        <h2 style=""color: #333; margin-top: 0;"">طلب إعادة ضبط كلمة المرور</h2>
        <p style=""color: #555; font-size: 16px; line-height: 1.5;"">لقد تلقينا طلباً لإعادة ضبط كلمة المرور الخاصة بحسابك. الرجاء النقر على الزر أدناه لاختيار كلمة مرور جديدة:</p>
        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{resetLink}"" 
               style=""display: inline-block; padding: 12px 25px; background-color: #f09d13; color: #fff; text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 16px;"">
               إعادة ضبط كلمة المرور
            </a>
        </div>
        <p style=""color: #888; font-size: 13px; line-height: 1.4; margin-bottom: 0;"">
            ستنتهي صلاحية هذا الرابط خلال 15 دقيقة. إذا لم تكن أنت من طلب إعادة ضبط كلمة المرور، يرجى تجاهل هذه الرسالة ولن يتم إجراء أي تغيير على حسابك.
        </p>
    </div>
    <div style=""text-align: center; margin-top: 20px; color: #999; font-size: 12px;"">
        &copy; {DateTime.UtcNow.Year} شيرين للسجاد. جميع الحقوق محفوظة.
    </div>
</div>";

        await _emailService.SendEmailAsync(request.Email, "طلب إعادة ضبط كلمة المرور - شيرين للسجاد", htmlBody, cancellationToken);
        _logger.LogInformation("Password reset email sent for {Email}.", request.Email);

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _resetPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(Error.Validation(
                "VALIDATION_ERROR",
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Result.Failure(Error.Validation("INVALID_TOKEN", "Invalid token or email."));

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        }
        catch (FormatException)
        {
            return Result.Failure(Error.Validation("INVALID_TOKEN", "The provided token is malformed."));
        }

        var resetResult = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        if (!resetResult.Succeeded)
        {
            var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("RESET_FAILED", errors));
        }

        var htmlBody = $@"
<div dir=""rtl"" style=""font-family: 'Tajawal', 'Segoe UI', Tahoma, Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #f9f9f9; padding: 20px; border-radius: 8px; border: 1px solid #e0e0e0;"">
    <div style=""text-align: center; margin-bottom: 20px;"">
        <h1 style=""color: #30584b; margin: 0; font-size: 28px;"">شيرين للسجاد</h1>
    </div>
    <div style=""background-color: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.05);"">
        <h2 style=""color: #333; margin-top: 0;"">تم تغيير كلمة المرور بنجاح</h2>
        <p style=""color: #555; font-size: 16px; line-height: 1.5;"">هذا تأكيد على أنه قد تم تغيير كلمة المرور الخاصة بحسابك بنجاح.</p>
        <p style=""color: #d9534f; font-weight: bold; font-size: 15px; margin-top: 20px; padding: 10px; background-color: #fdf2f2; border-radius: 5px; border-right: 4px solid #d9534f;"">
            إذا لم تقم بإجراء هذا التغيير، يرجى التواصل مع فريق الدعم الفني لدينا فوراً لحماية حسابك.
        </p>
    </div>
    <div style=""text-align: center; margin-top: 20px; color: #999; font-size: 12px;"">
        &copy; {DateTime.UtcNow.Year} شيرين للسجاد. جميع الحقوق محفوظة.
    </div>
</div>";

        await _emailService.SendEmailAsync(request.Email, "تأكيد تغيير كلمة المرور - شيرين للسجاد", htmlBody, cancellationToken);
        _logger.LogInformation("Password successfully reset for user {UserId}", user.Id);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result<AuthResponse>> RefreshTokenAsync(
        string refreshToken,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var tokenEntity = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (tokenEntity is null)
            return Result.Failure<AuthResponse>(Error.Validation("INVALID_TOKEN", "Invalid refresh token."));

        if (!tokenEntity.IsActive)
            return Result.Failure<AuthResponse>(Error.Validation("EXPIRED_TOKEN", "Refresh token is expired or revoked."));

        // Revoke the old token
        tokenEntity.Revoke(ipAddress);

        var roles = await _userManager.GetRolesAsync(tokenEntity.User);
        if (!roles.Contains(Roles.Customer))
            return Result.Failure<AuthResponse>(Error.Validation("INVALID_CREDENTIALS", "Invalid role."));

        return Result.Success(await BuildAuthResponseAsync(tokenEntity.User, ipAddress));
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user, string ipAddress = "0.0.0.0")
    {
        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtProvider.GenerateToken(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName ?? string.Empty,
            user.LastName  ?? string.Empty,
            roles);

        // Generate Refresh Token
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var refreshTokenString = Convert.ToBase64String(randomNumber);

        var refreshToken = RefreshToken.Create(
            userId: user.Id,
            token: refreshTokenString,
            expiresAt: DateTime.UtcNow.AddDays(30),
            createdByIp: ipAddress);

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken  = token,
            RefreshToken = refreshToken.Token,
            ExpiresIn    = _jwtSettings.ExpiryMinutes * 60,
            ExpiresAtUtc = refreshToken.ExpiresAt,
            UserId       = user.Id,
            Email        = user.Email ?? string.Empty,
            FullName     = user.DisplayName,
            Role         = roles.FirstOrDefault() ?? string.Empty
        };
    }
}
