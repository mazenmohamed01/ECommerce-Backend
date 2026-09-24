using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts.Profile;

namespace ECommerce.Application.Features.Profiles.Commands.ChangePassword;

public sealed record ChangePasswordCommand(string UserId, ChangePasswordRequest Request) : ICommand<Result>;
