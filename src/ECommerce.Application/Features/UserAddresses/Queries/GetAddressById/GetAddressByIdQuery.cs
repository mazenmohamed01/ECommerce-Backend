using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts.Addresses;
using System;

namespace ECommerce.Application.Features.UserAddresses.Queries.GetAddressById;

public sealed record GetAddressByIdQuery(Guid AddressId, string UserId) : IQuery<Result<AddressDto>>;
