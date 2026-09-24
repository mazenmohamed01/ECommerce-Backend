using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Admin-only authentication service.
/// Handles email/password login for administrators.
/// Google OAuth is explicitly NOT supported for admin accounts.
/// </summary>
public interface IAdminAuthService
{
    /// <summary>
    /// Authenticates an admin user using email and password.
    /// Returns a failure result if the user does not hold the Admin role.
    /// </summary>
    Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
