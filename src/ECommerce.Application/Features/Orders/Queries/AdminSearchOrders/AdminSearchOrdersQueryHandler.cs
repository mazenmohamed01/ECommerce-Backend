using AutoMapper;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Features.Orders.Queries.AdminSearchOrders;

internal sealed class AdminSearchOrdersQueryHandler : IQueryHandler<AdminSearchOrdersQuery, Result<PagedResponse<OrderSummaryResponse>>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;

    public AdminSearchOrdersQueryHandler(IOrderRepository orderRepository, IMapper mapper)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
    }

    public async Task<Result<PagedResponse<OrderSummaryResponse>>> Handle(AdminSearchOrdersQuery query, CancellationToken cancellationToken)
    {
        var request = query.Request;
        
        var filter = new OrderSearchFilter
        {
            Status = request.Status,
            PaymentMethod = request.PaymentMethod,
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            Keyword = request.Keyword,
            Page = request.PageNumber,
            PageSize = request.PageSize,
            SortBy = request.SortBy,
            SortDirection = request.SortDirection
        };

        var (items, totalCount) = await _orderRepository.SearchForAdminAsync(filter, cancellationToken);
        
        var dtos = _mapper.Map<List<OrderSummaryResponse>>(items);
        var response = new PagedResponse<OrderSummaryResponse>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        
        return Result.Success<PagedResponse<OrderSummaryResponse>>(response);
    }
}
