using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.AdminCustomers.Queries.SearchCustomers;

public sealed record SearchCustomersQuery(string? SearchTerm, int Page, int PageSize) : IQuery<Result<PagedResponse<CustomerResponse>>>;
