using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Constants;
using ECommerce.Infrastructure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services;

/// <summary>
/// Admin-only authentication service.
/// Only users with the Admin role can sign in through this service.
/// Google OAuth is explicitly excluded.
/// </summary>
internal sealed class AdminAuthService : IAdminAuthService
{
    private readonly UserManager<ApplicationUser>   _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtProvider                   _jwtProvider;
    private readonly IValidator<LoginRequest>       _loginValidator;
    private readonly Authentication.JwtSettings     _jwtSettings;
    private readonly ILogger<AdminAuthService>      _logger;

    public AdminAuthService(
        UserManager<ApplicationUser>   userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtProvider                   jwtProvider,
        IValidator<LoginRequest>       loginValidator,
        IOptions<Authentication.JwtSettings> jwtOptions,
        ILogger<AdminAuthService>      logger)
    {
        _userManager    = userManager;
        _signInManager  = signInManager;
        _jwtProvider    = jwtProvider;
        _loginValidator = loginValidator;
        _jwtSettings    = jwtOptions.Value;
        _logger         = logger;
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

        // ── Admin role check ───────────────────────────────────────────────────
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(Roles.Admin))
        {
            _logger.LogWarning(
                "Admin login attempt for non-admin user {Email}", request.Email);

            // Return the same generic error to prevent email enumeration
            return Result.Failure<AuthResponse>(Error.Validation(
                "INVALID_CREDENTIALS", "Invalid email or password."));
        }

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

        _logger.LogInformation("Admin logged in: {UserId} ({Email})", user.Id, user.Email);

        // ── Build token ────────────────────────────────────────────────────────
        var token = _jwtProvider.GenerateToken(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName ?? string.Empty,
            user.LastName  ?? string.Empty,
            roles);

        return Result.Success(new AuthResponse
        {
            AccessToken = token,
            ExpiresIn   = _jwtSettings.ExpiryMinutes * 60,
            UserId      = user.Id,
            Email       = user.Email ?? string.Empty,
            FullName    = user.DisplayName,
            Role        = roles.FirstOrDefault() ?? string.Empty
        });
    }
}
