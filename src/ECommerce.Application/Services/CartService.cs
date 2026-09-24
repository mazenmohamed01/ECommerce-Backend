using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public sealed class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CartService> _logger;

    public CartService(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<CartService> logger)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CartResponse>> GetCartAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, includeItems: true, cancellationToken);
        return MapToResponse(cart);
    }

    public async Task<Result<CartResponse>> AddItemAsync(string customerId, AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, includeItems: true, cancellationToken);
        bool isNewCart = false;

        if (cart is null)
        {
            cart = Cart.Create(customerId);
            isNewCart = true;
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        
        // Use the already loaded product if it exists in the cart, otherwise fetch it.
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

        // Fetch again to ensure all navigation properties (like Product and Images) are fully loaded
        var updatedCart = await _cartRepository.GetByCustomerIdAsync(customerId, includeItems: true, cancellationToken);
        return MapToResponse(updatedCart);
    }

    public async Task<Result<CartResponse>> UpdateItemQuantityAsync(string customerId, Guid cartItemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity == 0)
        {
            return await RemoveItemAsync(customerId, cartItemId, cancellationToken);
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
        return MapToResponse(updatedCart);
    }

    public async Task<Result<CartResponse>> RemoveItemAsync(string customerId, Guid cartItemId, CancellationToken cancellationToken = default)
    {
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
        return MapToResponse(updatedCart);
    }

    public async Task<Result> ClearCartAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetByCustomerIdAsync(customerId, includeItems: false, cancellationToken);
        if (cart is not null)
        {
            await _cartRepository.ClearItemsAsync(cart.Id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    private static CartResponse MapToResponse(Cart? cart)
    {
        if (cart is null)
        {
            return new CartResponse
            {
                CartId = Guid.Empty,
                Items = Array.Empty<CartItemResponse>(),
                SubTotal = 0,
                TotalItemsCount = 0,
                HasUnavailableItems = false
            };
        }

        var itemResponses = new List<CartItemResponse>();
        decimal subTotal = 0;
        int totalItemsCount = 0;
        bool hasUnavailable = false;

        foreach (var item in cart.Items.OrderBy(i => i.CreatedAt))
        {
            var product = item.Product;
            if (product is null) continue;

            bool isAvailable = product.IsActive && product.QuantityInStock >= item.Quantity;
            if (!isAvailable)
            {
                hasUnavailable = true;
            }

            var lineTotal = isAvailable ? product.Price * item.Quantity : 0;
            
            if (isAvailable)
            {
                subTotal += lineTotal;
            }
            
            totalItemsCount += item.Quantity;
            
            var mainImageUrl = product.Images.FirstOrDefault(img => img.IsMain)?.ImageUrl 
                               ?? product.Images.FirstOrDefault()?.ImageUrl;

            itemResponses.Add(new CartItemResponse
            {
                CartItemId = item.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                MainImageUrl = mainImageUrl,
                UnitPrice = product.Price,
                Quantity = item.Quantity,
                LineTotal = lineTotal,
                IsAvailable = isAvailable,
                AvailableStock = !isAvailable ? product.QuantityInStock : null
            });
        }

        return new CartResponse
        {
            CartId = cart.Id,
            Items = itemResponses,
            SubTotal = subTotal,
            TotalItemsCount = totalItemsCount,
            HasUnavailableItems = hasUnavailable
        };
    }
}
