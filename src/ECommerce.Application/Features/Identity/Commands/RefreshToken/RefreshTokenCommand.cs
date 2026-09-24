using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.Identity.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string Token, string IpAddress) : ICommand<Result<AuthResponse>>;
