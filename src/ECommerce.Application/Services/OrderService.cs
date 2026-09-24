using AutoMapper;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IStockService _stockService;
    private readonly IOrderNumberGenerator _orderNumberGenerator;
    private readonly IPaymentService _paymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IN8nIntegrationService _n8nIntegrationService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        IStockService stockService,
        IOrderNumberGenerator orderNumberGenerator,
        IPaymentService paymentService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IN8nIntegrationService n8nIntegrationService,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _stockService = stockService;
        _orderNumberGenerator = orderNumberGenerator;
        _paymentService = paymentService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _n8nIntegrationService = n8nIntegrationService;
        _logger = logger;
    }

    public async Task<Result<OrderResponse>> CheckoutAsync(
        string customerId, 
        CreateOrderRequest request, 
        CancellationToken cancellationToken = default)
    {
        // 1. Fetch Cart (outside transaction — read-only)
        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, true, cancellationToken);
        if (cart == null || !cart.Items.Any())
        {
            return Result.Failure<OrderResponse>(Error.Validation("EmptyCart", "Cannot checkout an empty cart."));
        }

        // Parse city from string to enum
        var parsedCity = request.City.Trim() switch
        {
            "الرياض" or "Riyadh" => SaudiCity.Riyadh,
            "جدة" or "Jeddah" => SaudiCity.Jeddah,
            "مكة" or "Mecca" or "مكة المكرمة" => SaudiCity.Mecca,
            "المدينة" or "Medina" or "المدينة المنورة" => SaudiCity.Medina,
            "الدمام" or "Dammam" => SaudiCity.Dammam,
            "الخبر" or "Khobar" => SaudiCity.Khobar,
            "الطائف" or "Taif" => SaudiCity.Taif,
            "بريدة" or "Buraidah" => SaudiCity.Buraidah,
            "تبوك" or "Tabuk" => SaudiCity.Tabuk,
            "أبها" or "Abha" => SaudiCity.Abha,
            "خميس مشيط" or "KhamisMushait" or "خميس" => SaudiCity.KhamisMushait,
            "حائل" or "Hail" => SaudiCity.Hail,
            "نجران" or "Najran" => SaudiCity.Najran,
            "الجبيل" or "Jubail" => SaudiCity.Jubail,
            "ينبع" or "Yanbu" => SaudiCity.Yanbu,
            _ => Enum.TryParse<SaudiCity>(request.City, true, out var c) ? c : SaudiCity.Riyadh // Default fallback
        };

        var address = new ShippingAddress(
            request.Street, 
            request.District, 
            parsedCity, 
            request.State, 
            request.PostalCode);

        // 2. Generate Order Number (outside transaction — Redis atomic counter)
        var orderNumber = await _orderNumberGenerator.GenerateAsync(cancellationToken);

        // 3. Create Order entity shell (no DB write yet)
        var order = Order.Create(
            orderNumber: orderNumber,
            customerId: customerId,
            customerName: request.CustomerName,
            customerPhone: request.CustomerPhone,
            customerEmail: "email@example.com",
            shippingAddress: address,
            paymentMethod: request.PaymentMethod,
            subTotal: cart.Items.Sum(i => i.Product!.Price * i.Quantity),
            shippingCost: 50m,
            discountAmount: 0m,
            notes: request.Notes);

        // 4. Execute the entire transactional block inside CreateExecutionStrategy
        //    so it is compatible with NpgsqlRetryingExecutionStrategy.
        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync<Result<OrderResponse>>(async ct =>
            {
                // Reserve stock + build order items atomically within one transaction
                foreach (var cartItem in cart.Items)
                {
                    var stockResult = await _stockService.ReserveStockAsync(cartItem.ProductId, cartItem.Quantity, ct);
                    if (stockResult.IsFailure)
                        return Result.Failure<OrderResponse>(stockResult.Error);

                    var orderItem = OrderItem.Create(
                        orderId: order.Id,
                        productId: cartItem.ProductId,
                        productName: cartItem.Product!.Name,
                        productSku: cartItem.Product!.Sku,
                        unitPrice: cartItem.Product!.Price,
                        quantity: cartItem.Quantity);

                    order.AddItem(orderItem);
                }

                await _orderRepository.AddAsync(order, ct);
                await _cartRepository.DeleteAsync(cart, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                _logger.LogInformation("Successfully created Order {OrderNumber} for customer {CustomerId}", order.OrderNumber, customerId);
                
                var orderResponse = _mapper.Map<OrderResponse>(order);
                
                // Fire and forget the n8n notification so it doesn't block the request
                _ = _n8nIntegrationService.SendOrderCreatedEventAsync(orderResponse);

                return Result.Success<OrderResponse>(orderResponse);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during checkout for customer {CustomerId}", customerId);
            return Result.Failure<OrderResponse>(Error.Failure("CheckoutFailed", "An unexpected error occurred during checkout."));
        }
    }

    public async Task<Result<OrderResponse>> GetByIdAsync(Guid id, string? customerId = null, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, true, cancellationToken);
        if (order == null)
            return Result.Failure<OrderResponse>(Error.NotFound("OrderNotFound", $"Order {id} not found."));

        if (!string.IsNullOrEmpty(customerId) && order.CustomerId != customerId)
            return Result.Failure<OrderResponse>(Error.Unauthorized("AccessDenied", "You do not have permission to view this order."));

        return Result.Success<OrderResponse>(_mapper.Map<OrderResponse>(order));
    }

    public async Task<Result<PagedResponse<OrderSummaryResponse>>> GetCustomerOrdersAsync(string customerId, PaginationRequest request, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _orderRepository.GetByCustomerIdAsync(customerId, request.PageNumber, request.PageSize, cancellationToken);
        
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

    public async Task<Result<PagedResponse<OrderSummaryResponse>>> AdminSearchOrdersAsync(AdminOrderSearchRequest request, CancellationToken cancellationToken = default)
    {
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

    public async Task<Result<OrderResponse>> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusRequest request, string changedByAdminId, CancellationToken cancellationToken = default)
    {
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

    public async Task<Result<PaymentInitiationResponse>> InitiatePaymentAsync(Guid orderId, string customerId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, true, cancellationToken);
        if (order == null)
            return Result.Failure<PaymentInitiationResponse>(Error.NotFound("OrderNotFound", $"Order {orderId} not found."));

        if (order.CustomerId != customerId)
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

        // Allow any transition (admin can correct mistakes)
        return Result.Success();
    }
}
