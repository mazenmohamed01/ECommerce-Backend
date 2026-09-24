using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.Orders.Queries.GetCustomerOrders;

public sealed record GetCustomerOrdersQuery(string CustomerId, PaginationRequest Request) : IQuery<Result<PagedResponse<OrderSummaryResponse>>>;
