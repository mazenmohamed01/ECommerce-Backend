using ECommerce.Application.Common;
using ECommerce.Application.Contracts.Profile;

namespace ECommerce.Application.Interfaces;

public interface IProfileService
{
    Task<Result<ProfileResponse>> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<ProfileResponse>> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
