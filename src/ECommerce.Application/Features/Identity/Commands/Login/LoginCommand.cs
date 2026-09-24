using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.Identity.Commands.Login;

public sealed record LoginCommand(LoginRequest Request) : ICommand<Result<AuthResponse>>;
