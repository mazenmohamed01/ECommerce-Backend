using AutoMapper;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Features.Orders.Commands.UpdateOrderStatus;

internal sealed class UpdateOrderStatusCommandHandler : ICommandHandler<UpdateOrderStatusCommand, Result<OrderResponse>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IStockService _stockService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateOrderStatusCommandHandler> _logger;

    public UpdateOrderStatusCommandHandler(
        IOrderRepository orderRepository,
        IStockService stockService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateOrderStatusCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _stockService = stockService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<OrderResponse>> Handle(UpdateOrderStatusCommand command, CancellationToken cancellationToken)
    {
        var id = command.OrderId;
        var request = command.Request;
        var changedByAdminId = command.ChangedByAdminId;

        var order = await _orderRepository.GetByIdAsync(id, true, cancellationToken);
        if (order == null)
            return Result.Failure<OrderResponse>(Error.NotFound("OrderNotFound", $"Order {id} not found."));

        var transitionValidation = ValidateStatusTransition(order.OrderStatus, request.NewStatus);
        if (transitionValidation.IsFailure)
            return Result.Failure<OrderResponse>(transitionValidation.Error);

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync<Result<OrderResponse>>(async ct =>
            {
                var oldStatus = order.OrderStatus;
                order.UpdateStatus(request.NewStatus, changedByAdminId, request.Reason);

                if (request.NewStatus == OrderStatus.Cancelled)
                {
                    foreach (var item in order.Items)
                    {
                        if (oldStatus == OrderStatus.Pending)
                        {
                            var releaseResult = await _stockService.ReleaseStockAsync(item.ProductId, item.Quantity, ct);
                            if (releaseResult.IsFailure)
                                return Result.Failure<OrderResponse>(releaseResult.Error);
                        }
                        else if (oldStatus == OrderStatus.Confirmed || oldStatus == OrderStatus.Processing)
                        {
                            var restoreResult = await _stockService.RestoreStockAsync(item.ProductId, item.Quantity, ct);
                            if (restoreResult.IsFailure)
                                return Result.Failure<OrderResponse>(restoreResult.Error);
                        }
                    }
                }
                else if (request.NewStatus == OrderStatus.Confirmed && oldStatus == OrderStatus.Pending)
                {
                    foreach (var item in order.Items)
                    {
                        var confirmResult = await _stockService.ConfirmStockAsync(item.ProductId, item.Quantity, ct);
                        if (confirmResult.IsFailure)
                            return Result.Failure<OrderResponse>(confirmResult.Error);
                    }
                }

                await _unitOfWork.SaveChangesAsync(ct);

                _logger.LogInformation("Admin {AdminId} updated order {OrderId} to {Status}", changedByAdminId, id, request.NewStatus);
                return Result.Success<OrderResponse>(_mapper.Map<OrderResponse>(order));
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order status for {OrderId}", id);
            return Result.Failure<OrderResponse>(Error.Failure("UpdateFailed", "An unexpected error occurred while updating the order status."));
        }
    }

    private Result ValidateStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        if (currentStatus == newStatus)
            return Result.Success();

        if (newStatus == OrderStatus.Cancelled)
        {
            if (currentStatus == OrderStatus.Shipped)
                return Result.Failure(Error.Validation("INVALID_STATUS_TRANSITION", "CANNOT_CANCEL_SHIPPED_ORDER"));
            if (currentStatus == OrderStatus.Delivered)
                return Result.Failure(Error.Validation("INVALID_STATUS_TRANSITION", "CANNOT_CANCEL_DELIVERED_ORDER"));
                
            return Result.Success();
        }

        return Result.Success();
    }
}
