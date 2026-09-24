using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.AdminCustomers.Queries.GetCustomerDetails;

public sealed record GetCustomerDetailsQuery(string CustomerId) : IQuery<Result<CustomerDetailsResponse>>;
