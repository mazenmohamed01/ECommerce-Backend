using AutoMapper;
using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ECommerce.Application.Features.Orders.Commands.CheckoutOrder;

internal sealed class CheckoutOrderCommandHandler : ICommandHandler<CheckoutOrderCommand, Result<OrderResponse>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IStockService _stockService;
    private readonly IOrderNumberGenerator _orderNumberGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IN8nIntegrationService _n8nIntegrationService;
    private readonly ILogger<CheckoutOrderCommandHandler> _logger;

    public CheckoutOrderCommandHandler(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        IStockService stockService,
        IOrderNumberGenerator orderNumberGenerator,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IN8nIntegrationService n8nIntegrationService,
        ILogger<CheckoutOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _stockService = stockService;
        _orderNumberGenerator = orderNumberGenerator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _n8nIntegrationService = n8nIntegrationService;
        _logger = logger;
    }

    public async Task<Result<OrderResponse>> Handle(CheckoutOrderCommand command, CancellationToken cancellationToken)
    {
        var customerId = command.CustomerId;
        var request = command.Request;

        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, true, cancellationToken);
        if (cart == null || !cart.Items.Any())
        {
            return Result.Failure<OrderResponse>(Error.Validation("EmptyCart", "Cannot checkout an empty cart."));
        }

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
            _ => Enum.TryParse<SaudiCity>(request.City, true, out var c) ? c : SaudiCity.Riyadh
        };

        var address = new ShippingAddress(
            request.Street, 
            request.District, 
            parsedCity, 
            request.State, 
            request.PostalCode);

        var orderNumber = await _orderNumberGenerator.GenerateAsync(cancellationToken);

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

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync<Result<OrderResponse>>(async ct =>
            {
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
}
