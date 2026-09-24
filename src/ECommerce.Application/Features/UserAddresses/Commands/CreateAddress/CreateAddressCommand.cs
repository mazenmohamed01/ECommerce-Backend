using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts.Addresses;

namespace ECommerce.Application.Features.UserAddresses.Commands.CreateAddress;

public sealed record CreateAddressCommand(string UserId, CreateAddressRequest Request) : ICommand<Result<AddressDto>>;
