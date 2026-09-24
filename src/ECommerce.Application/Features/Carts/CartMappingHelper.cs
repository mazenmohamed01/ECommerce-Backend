using ECommerce.Application.Contracts;
using ECommerce.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace ECommerce.Application.Features.Carts;

internal static class CartMappingHelper
{
    public static CartResponse MapToResponse(Cart? cart)
    {
        if (cart is null)
        {
            return new CartResponse
            {
                CartId = System.Guid.Empty,
                Items = System.Array.Empty<CartItemResponse>(),
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
