using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts.Addresses;
using System.Collections.Generic;

namespace ECommerce.Application.Features.UserAddresses.Queries.GetUserAddresses;

public sealed record GetUserAddressesQuery(string UserId) : IQuery<Result<IEnumerable<AddressDto>>>;
