using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.Identity.Commands.GoogleLogin;

public sealed record GoogleLoginCommand(GoogleLoginRequest Request) : ICommand<Result<AuthResponse>>;
