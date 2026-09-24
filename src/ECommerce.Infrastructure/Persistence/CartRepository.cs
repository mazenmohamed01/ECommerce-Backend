using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

/// <summary>
/// Infrastructure implementation of the Cart data access layer.
/// </summary>
public sealed class CartRepository : Repository<Cart>, ICartRepository
{
    public CartRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Cart?> GetByCustomerIdAsync(string customerId, bool includeItems = true, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (includeItems)
        {
            query = query
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.Images); // Include product images for MainImageUrl
        }

        return await query.FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
    }

    public async Task<CartItem?> GetItemAsync(Guid cartId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await Context.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cartId && i.ProductId == productId, cancellationToken);
    }

    public async Task AddItemAsync(CartItem item, CancellationToken cancellationToken = default)
    {
        await Context.CartItems.AddAsync(item, cancellationToken);
    }

    public async Task RemoveItemAsync(Guid cartItemId, CancellationToken cancellationToken = default)
    {
        var item = await Context.CartItems.FindAsync([cartItemId], cancellationToken);
        if (item != null)
        {
            Context.CartItems.Remove(item);
        }
    }

    public async Task ClearItemsAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        var items = await Context.CartItems
            .Where(i => i.CartId == cartId)
            .ToListAsync(cancellationToken);
            
        if (items.Any())
        {
            Context.CartItems.RemoveRange(items);
        }
    }
}
