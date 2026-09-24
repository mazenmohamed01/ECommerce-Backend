using ECommerce.Application.Contracts;
using ECommerce.Application.Features.AdminAuth.Commands.AdminLogin;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Admin authentication endpoints.
/// Only email/password login is supported — Google OAuth is not available for admin accounts.
/// </summary>
[ApiController]
[Route("api/admin/auth")]
[Produces("application/json")]
public sealed class AdminAuthController : BaseApiController
{

    /// <summary>
    /// Authenticates an admin user using email and password.
    /// Returns a JWT access token on success.
    /// The endpoint returns an identical error for invalid credentials and non-admin emails
    /// to prevent user enumeration.
    /// </summary>
    /// <response code="200">Login successful. Returns access token and admin profile.</response>
    /// <response code="400">Invalid credentials or validation error.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new AdminLoginCommand(request), cancellationToken);
        return HandleResult(result);
    }
}
