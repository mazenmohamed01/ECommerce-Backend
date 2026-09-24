using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using System.Linq;

namespace ECommerce.Application.Features.Carts.Commands.AddItemToCart;

internal sealed class AddItemToCartCommandHandler : ICommandHandler<AddItemToCartCommand, Result<CartResponse>>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddItemToCartCommandHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CartResponse>> Handle(AddItemToCartCommand command, CancellationToken cancellationToken)
    {
        var customerId = command.CustomerId;
        var request = command.Request;

        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, includeItems: true, cancellationToken);
        bool isNewCart = false;

        if (cart is null)
        {
            cart = Cart.Create(customerId);
            isNewCart = true;
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        
        var product = existingItem?.Product ?? await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        
        if (product is null)
        {
            return Result.Failure<CartResponse>(Error.NotFound("PRODUCT_NOT_FOUND", "The requested product does not exist."));
        }

        if (!product.IsActive)
        {
            return Result.Failure<CartResponse>(Error.Validation("PRODUCT_UNAVAILABLE", "The requested product is inactive."));
        }

        if (product.QuantityInStock == 0)
        {
            return Result.Failure<CartResponse>(Error.Validation("PRODUCT_OUT_OF_STOCK", "The requested product is out of stock."));
        }

        int newQuantity = (existingItem?.Quantity ?? 0) + request.Quantity;

        if (newQuantity > product.QuantityInStock)
        {
            return Result.Failure<CartResponse>(Error.Validation("INSUFFICIENT_STOCK", $"Requested quantity exceeds available stock ({product.QuantityInStock})."));
        }

        cart.AddItem(request.ProductId, request.Quantity);

        if (isNewCart)
        {
            await _cartRepository.AddAsync(cart, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedCart = await _cartRepository.GetByCustomerIdAsync(customerId, includeItems: true, cancellationToken);
        return CartMappingHelper.MapToResponse(updatedCart);
    }
}
