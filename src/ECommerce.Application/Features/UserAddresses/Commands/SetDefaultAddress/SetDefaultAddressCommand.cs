using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using System;

namespace ECommerce.Application.Features.UserAddresses.Commands.SetDefaultAddress;

public sealed record SetDefaultAddressCommand(Guid AddressId, string UserId) : ICommand<Result>;
