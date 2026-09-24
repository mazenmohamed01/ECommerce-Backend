using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Interfaces;
using System.Linq;

namespace ECommerce.Application.Features.Carts.Commands.RemoveItemFromCart;

internal sealed class RemoveItemFromCartCommandHandler : ICommandHandler<RemoveItemFromCartCommand, Result<CartResponse>>
{
    private readonly ICartRepository _cartRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveItemFromCartCommandHandler(ICartRepository cartRepository, IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CartResponse>> Handle(RemoveItemFromCartCommand command, CancellationToken cancellationToken)
    {
        var customerId = command.CustomerId;
        var cartItemId = command.CartItemId;

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

        cart.RemoveItem(cartItemId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedCart = await _cartRepository.GetByCustomerIdAsync(customerId, includeItems: true, cancellationToken);
        return CartMappingHelper.MapToResponse(updatedCart);
    }
}
