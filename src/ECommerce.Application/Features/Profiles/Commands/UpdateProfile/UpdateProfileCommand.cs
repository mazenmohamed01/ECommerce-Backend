using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts.Profile;

namespace ECommerce.Application.Features.Profiles.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(string UserId, UpdateProfileRequest Request) : ICommand<Result<ProfileResponse>>;
