using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Features.Carts.Queries.GetCart;

internal sealed class GetCartQueryHandler : IQueryHandler<GetCartQuery, Result<CartResponse>>
{
    private readonly ICartRepository _cartRepository;

    public GetCartQueryHandler(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<Result<CartResponse>> Handle(GetCartQuery query, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByCustomerIdAsync(query.CustomerId, includeItems: true, cancellationToken);
        return CartMappingHelper.MapToResponse(cart);
    }
}
