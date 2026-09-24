using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Interfaces;
using MediatR;
using System.Linq;

namespace ECommerce.Application.Features.Carts.Commands.UpdateCartItemQuantity;

internal sealed class UpdateCartItemQuantityCommandHandler : ICommandHandler<UpdateCartItemQuantityCommand, Result<CartResponse>>
{
    private readonly ICartRepository _cartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISender _sender;

    public UpdateCartItemQuantityCommandHandler(
        ICartRepository cartRepository,
        IUnitOfWork unitOfWork,
        ISender sender)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
        _sender = sender;
    }

    public async Task<Result<CartResponse>> Handle(UpdateCartItemQuantityCommand command, CancellationToken cancellationToken)
    {
        var customerId = command.CustomerId;
        var cartItemId = command.CartItemId;
        var request = command.Request;

        if (request.Quantity == 0)
        {
            return await _sender.Send(new RemoveItemFromCart.RemoveItemFromCartCommand(customerId, cartItemId), cancellationToken);
        }

        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, includeItems: true, cancellationToken);
        if (cart is null)
        {
            return Result.Failure<CartResponse>(Error.NotFound("CART_NOT_FOUND", "Cart not found."));
        }

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item is null)
        {
            return Result.Failure<CartResponse>(Error.NotFound("CART_ITEM_NOT_FOUND", "Cart item not found."));
        }

        var product = item.Product;
        if (product is null || !product.IsActive)
        {
            return Result.Failure<CartResponse>(Error.Validation("PRODUCT_UNAVAILABLE", "The requested product is unavailable."));
        }

        if (request.Quantity > product.QuantityInStock)
        {
            return Result.Failure<CartResponse>(Error.Validation("INSUFFICIENT_STOCK", $"Requested quantity exceeds available stock ({product.QuantityInStock})."));
        }

        cart.UpdateItemQuantity(cartItemId, request.Quantity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedCart = await _cartRepository.GetByCustomerIdAsync(customerId, includeItems: true, cancellationToken);
        return CartMappingHelper.MapToResponse(updatedCart);
    }
}
