using AutoMapper;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Features.Orders.Queries.GetOrderById;

internal sealed class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, Result<OrderResponse>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository, IMapper mapper)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
    }

    public async Task<Result<OrderResponse>> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(query.Id, true, cancellationToken);
        if (order == null)
            return Result.Failure<OrderResponse>(Error.NotFound("OrderNotFound", $"Order {query.Id} not found."));

        if (!string.IsNullOrEmpty(query.CustomerId) && order.CustomerId != query.CustomerId)
            return Result.Failure<OrderResponse>(Error.Unauthorized("AccessDenied", "You do not have permission to view this order."));

        return Result.Success<OrderResponse>(_mapper.Map<OrderResponse>(order));
    }
}
