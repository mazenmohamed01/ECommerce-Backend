using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Features.Orders.Commands.InitiateOrderPayment;

internal sealed class InitiateOrderPaymentCommandHandler : ICommandHandler<InitiateOrderPaymentCommand, Result<PaymentInitiationResponse>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentService _paymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InitiateOrderPaymentCommandHandler> _logger;

    public InitiateOrderPaymentCommandHandler(
        IOrderRepository orderRepository,
        IPaymentService paymentService,
        IUnitOfWork unitOfWork,
        ILogger<InitiateOrderPaymentCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _paymentService = paymentService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PaymentInitiationResponse>> Handle(InitiateOrderPaymentCommand command, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId, true, cancellationToken);
        if (order == null)
            return Result.Failure<PaymentInitiationResponse>(Error.NotFound("OrderNotFound", $"Order {command.OrderId} not found."));

        if (order.CustomerId != command.CustomerId)
            return Result.Failure<PaymentInitiationResponse>(Error.Unauthorized("AccessDenied", "You do not have permission to pay for this order."));

        if (order.PaymentMethod != PaymentMethod.Online)
            return Result.Failure<PaymentInitiationResponse>(Error.Validation("PaymentNotApplicable", "Payment can only be initiated for online orders."));

        if (order.PaymentStatus != PaymentStatus.Pending)
            return Result.Failure<PaymentInitiationResponse>(Error.Validation("OrderAlreadyPaid", "This order has already been paid or failed."));

        var paymentResult = await _paymentService.InitiatePaymentAsync(order, cancellationToken);
        if (paymentResult.IsFailure)
            return paymentResult;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await _orderRepository.UpdatePaymentDetailsAsync(order.Id, paymentResult.Value.PaymentId, "Moyasar", ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);

            return paymentResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save payment initiation details for order {OrderId}", order.Id);
            return Result.Failure<PaymentInitiationResponse>(Error.Failure("PaymentUpdateFailed", "Failed to save payment tracking details."));
        }
    }
}
