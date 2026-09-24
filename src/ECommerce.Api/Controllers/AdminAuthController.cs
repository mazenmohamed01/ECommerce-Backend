using ECommerce.Api.Extensions;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Admin authentication endpoints.
/// Only email/password login is supported — Google OAuth is not available for admin accounts.
/// </summary>
[ApiController]
[Route("api/admin/auth")]
[Produces("application/json")]
public sealed class AdminAuthController : ControllerBase
{
    private readonly IAdminAuthService _adminAuthService;

    public AdminAuthController(IAdminAuthService adminAuthService)
    {
        _adminAuthService = adminAuthService;
    }

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
        var result = await _adminAuthService.LoginAsync(request, cancellationToken);
        return this.Match(result);
    }
}
