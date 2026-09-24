using ECommerce.Application.Common;
using ECommerce.Application.Contracts.Profile;
using ECommerce.Application.Interfaces;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

using ECommerce.Application.Features.Profiles.Queries.GetProfile;
using ECommerce.Application.Features.Profiles.Commands.UpdateProfile;
using ECommerce.Application.Features.Profiles.Commands.ChangePassword;
using MediatR;
namespace ECommerce.Infrastructure.Services;

internal sealed class ProfileCommandHandlers : 
    IRequestHandler<GetCustomerProfileQuery, Result<ProfileResponse>>,
    IRequestHandler<UpdateProfileCommand, Result<ProfileResponse>>,
    IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileCommandHandlers(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<ProfileResponse>> Handle(GetCustomerProfileQuery command, CancellationToken cancellationToken)
    {
        var userId = command.UserId;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<ProfileResponse>(Error.NotFound("User.NotFound", "User not found."));

        var roles = await _userManager.GetRolesAsync(user);

        return Result.Success(new ProfileResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.DisplayName,
            user.PhoneNumber,
            roles.FirstOrDefault(),
            user.CreatedAt.ToString("o")
        ));
    }

    public async Task<Result<ProfileResponse>> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var userId = command.UserId;
        var request = command.Request;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<ProfileResponse>(Error.NotFound("User.NotFound", "User not found."));

        // Split FullName to FirstName and LastName
        var nameParts = request.FullName?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        user.FirstName = nameParts?.Length > 0 ? nameParts[0] : null;
        user.LastName = nameParts?.Length > 1 ? nameParts[1] : null;

        user.PhoneNumber = request.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<ProfileResponse>(Error.Failure("User.UpdateFailed", $"Failed to update profile: {errors}"));
        }

        return await Handle(new GetCustomerProfileQuery(userId), cancellationToken);
    }

    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var userId = command.UserId;
        var request = command.Request;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(Error.NotFound("User.NotFound", "User not found."));

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("User.ChangePasswordFailed", $"Failed to change password: {errors}"));
        }
        
        return Result.Success();
    }
}
