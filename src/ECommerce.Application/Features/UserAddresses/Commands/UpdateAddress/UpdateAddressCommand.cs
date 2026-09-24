using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts.Addresses;
using System;

namespace ECommerce.Application.Features.UserAddresses.Commands.UpdateAddress;

public sealed record UpdateAddressCommand(Guid AddressId, string UserId, UpdateAddressRequest Request) : ICommand<Result<AddressDto>>;
