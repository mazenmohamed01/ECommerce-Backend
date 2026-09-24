using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.AdminAuth.Commands.AdminLogin;

public sealed record AdminLoginCommand(LoginRequest Request) : ICommand<Result<AuthResponse>>;
