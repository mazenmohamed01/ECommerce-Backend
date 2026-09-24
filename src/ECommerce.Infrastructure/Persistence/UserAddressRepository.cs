using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

internal sealed class UserAddressRepository : Repository<UserAddress>, IUserAddressRepository
{
    public UserAddressRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<UserAddress>> GetUserAddressesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await Context.UserAddresses
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserAddress?> GetUserAddressByIdAsync(Guid addressId, string userId, CancellationToken cancellationToken = default)
    {
        return await Context.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId, cancellationToken);
    }

    public async Task<bool> HasAddressesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await Context.UserAddresses.AnyAsync(a => a.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<UserAddress>> GetDefaultAddressesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await Context.UserAddresses
            .Where(a => a.UserId == userId && a.IsDefault)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserAddress?> GetLatestAddressAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await Context.UserAddresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
