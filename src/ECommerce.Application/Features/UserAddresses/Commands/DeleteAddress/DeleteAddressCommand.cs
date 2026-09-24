using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using System;

namespace ECommerce.Application.Features.UserAddresses.Commands.DeleteAddress;

public sealed record DeleteAddressCommand(Guid AddressId, string UserId) : ICommand<Result>;
