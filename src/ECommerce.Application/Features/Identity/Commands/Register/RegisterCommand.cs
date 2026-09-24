using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.Identity.Commands.Register;

public sealed record RegisterCommand(RegisterRequest Request) : ICommand<Result<AuthResponse>>;
