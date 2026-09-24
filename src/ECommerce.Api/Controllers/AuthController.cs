using ECommerce.Application.Contracts;
using ECommerce.Application.Features.Identity.Commands.Register;
using ECommerce.Application.Features.Identity.Commands.Login;
using ECommerce.Application.Features.Identity.Commands.GoogleLogin;
using ECommerce.Application.Features.Identity.Commands.ForgotPassword;
using ECommerce.Application.Features.Identity.Commands.ResetPassword;
using ECommerce.Application.Features.Identity.Commands.RefreshToken;
using ECommerce.Application.Features.Identity.Queries.GetProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Customer authentication endpoints.
/// Handles registration, email/password login, Google OAuth, and profile retrieval.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : BaseApiController
{

    /// <summary>Registers a new customer using email and password.</summary>
    /// <response code="200">Registration successful. Returns access token and user profile.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RegisterCommand(request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Authenticates a customer using email and password.</summary>
    /// <response code="200">Login successful. Returns access token and user profile.</response>
    /// <response code="400">Invalid credentials or validation error.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new LoginCommand(request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Authenticates a customer using a Google ID token.
    /// Auto-creates a new account if the email does not exist.
    /// Links the Google account to an existing email/password account if it does exist.
    /// </summary>
    /// <response code="200">Login successful. Returns access token and user profile.</response>
    /// <response code="400">Invalid or expired Google token.</response>
    [HttpPost("google-login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GoogleLogin(
        [FromBody] GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GoogleLoginCommand(request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Returns the profile of the currently authenticated user.</summary>
    /// <response code="200">User profile retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var result = await Sender.Send(new GetProfileQuery(userId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Requests a password reset email.</summary>
    /// <response code="200">Returns a generic success message to prevent user enumeration.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="429">Rate limit exceeded.</response>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-policy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ForgotPasswordCommand(request), cancellationToken);
        
        if (result.IsSuccess)
        {
            return Ok(new { message = "If that email address is in our database, we will send you an email to reset your password." });
        }

        return HandleResult(result);
    }

    /// <summary>Resets a customer's password using a valid token.</summary>
    /// <response code="200">Password successfully reset.</response>
    /// <response code="400">Invalid token, mismatched passwords, or validation error.</response>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ResetPasswordCommand(request), cancellationToken);
        
        if (result.IsSuccess)
        {
            return Ok(new { message = "Password has been successfully reset." });
        }

        return HandleResult(result);
    }

    /// <summary>Refreshes the access token using a valid refresh token.</summary>
    /// <response code="200">Token refreshed successfully.</response>
    /// <response code="400">Invalid or expired refresh token.</response>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        var result = await Sender.Send(new RefreshTokenCommand(request.RefreshToken, ipAddress), cancellationToken);
        return HandleResult(result);
    }
}
