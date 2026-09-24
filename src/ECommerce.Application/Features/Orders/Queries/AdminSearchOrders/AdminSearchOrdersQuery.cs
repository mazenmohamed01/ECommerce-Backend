using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.Orders.Queries.AdminSearchOrders;

public sealed record AdminSearchOrdersQuery(AdminOrderSearchRequest Request) : IQuery<Result<PagedResponse<OrderSummaryResponse>>>;
