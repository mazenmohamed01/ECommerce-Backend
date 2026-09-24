using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Features.Carts.Commands.ClearCart;

internal sealed class ClearCartCommandHandler : ICommandHandler<ClearCartCommand, Result>
{
    private readonly ICartRepository _cartRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ClearCartCommandHandler(ICartRepository cartRepository, IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ClearCartCommand command, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByCustomerIdAsync(command.CustomerId, includeItems: false, cancellationToken);
        if (cart is not null)
        {
            await _cartRepository.ClearItemsAsync(cart.Id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
