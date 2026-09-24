using AutoMapper;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Features.Orders.Queries.GetCustomerOrders;

internal sealed class GetCustomerOrdersQueryHandler : IQueryHandler<GetCustomerOrdersQuery, Result<PagedResponse<OrderSummaryResponse>>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;

    public GetCustomerOrdersQueryHandler(IOrderRepository orderRepository, IMapper mapper)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
    }

    public async Task<Result<PagedResponse<OrderSummaryResponse>>> Handle(GetCustomerOrdersQuery query, CancellationToken cancellationToken)
    {
        var request = query.Request;
        var (items, totalCount) = await _orderRepository.GetByCustomerIdAsync(query.CustomerId, request.PageNumber, request.PageSize, cancellationToken);
        
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
