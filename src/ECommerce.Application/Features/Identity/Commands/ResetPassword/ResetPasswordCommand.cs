using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.Identity.Commands.ResetPassword;

public sealed record ResetPasswordCommand(ResetPasswordRequest Request) : ICommand<Result>;
