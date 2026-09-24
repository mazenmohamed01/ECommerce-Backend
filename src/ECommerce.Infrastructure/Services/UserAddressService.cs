using ECommerce.Application.Contracts.Addresses;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services;

internal sealed class UserAddressService : IUserAddressService
{
    private readonly ApplicationDbContext _dbContext;

    public UserAddressService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<AddressDto>> GetUserAddressesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserAddresses
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AddressDto(
                a.Id.ToString(),
                a.Title,
                a.Street,
                a.District,
                a.City,
                a.State,
                a.PostalCode,
                a.Country,
                a.IsDefault
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<AddressDto> GetAddressByIdAsync(string addressId, string userId, CancellationToken cancellationToken = default)
    {
        var parsedAddressId = Guid.Parse(addressId);
        var address = await _dbContext.UserAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == parsedAddressId && a.UserId == userId, cancellationToken);

        if (address is null)
            throw new KeyNotFoundException("Address not found.");

        return new AddressDto(
            address.Id.ToString(),
            address.Title,
            address.Street,
            address.District,
            address.City,
            address.State,
            address.PostalCode,
            address.Country,
            address.IsDefault
        );
    }

    public async Task<AddressDto> CreateAddressAsync(string userId, CreateAddressRequest request, CancellationToken cancellationToken = default)
    {
        bool hasAddresses = await _dbContext.UserAddresses.AnyAsync(a => a.UserId == userId, cancellationToken);
        bool isDefault = request.IsDefault || !hasAddresses; // Force default if it's the first address

        if (isDefault && hasAddresses)
        {
            await UnsetOtherDefaultsAsync(userId, cancellationToken);
        }

        var address = UserAddress.Create(
            userId,
            request.Title,
            request.Street,
            request.District,
            request.City,
            request.State,
            request.PostalCode,
            request.Country,
            isDefault
        );

        _dbContext.UserAddresses.Add(address);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AddressDto(
            address.Id.ToString(),
            address.Title,
            address.Street,
            address.District,
            address.City,
            address.State,
            address.PostalCode,
            address.Country,
            address.IsDefault
        );
    }

    public async Task<AddressDto> UpdateAddressAsync(string addressId, string userId, UpdateAddressRequest request, CancellationToken cancellationToken = default)
    {
        var parsedAddressId = Guid.Parse(addressId);
        var address = await _dbContext.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == parsedAddressId && a.UserId == userId, cancellationToken);

        if (address is null)
            throw new KeyNotFoundException("Address not found.");

        if (request.IsDefault && !address.IsDefault)
        {
            await UnsetOtherDefaultsAsync(userId, cancellationToken);
            address.SetDefault(true);
        }
        else if (!request.IsDefault && address.IsDefault)
        {
            // If they are un-defaulting the only default address, we should prevent it, but we'll allow it for now
            // or we could throw an exception if they don't have another default.
            address.SetDefault(false);
        }

        address.Update(
            request.Title,
            request.Street,
            request.District,
            request.City,
            request.State,
            request.PostalCode,
            request.Country
        );

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AddressDto(
            address.Id.ToString(),
            address.Title,
            address.Street,
            address.District,
            address.City,
            address.State,
            address.PostalCode,
            address.Country,
            address.IsDefault
        );
    }

    public async Task DeleteAddressAsync(string addressId, string userId, CancellationToken cancellationToken = default)
    {
        var parsedAddressId = Guid.Parse(addressId);
        var address = await _dbContext.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == parsedAddressId && a.UserId == userId, cancellationToken);

        if (address is null)
            throw new KeyNotFoundException("Address not found.");

        _dbContext.UserAddresses.Remove(address);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (address.IsDefault)
        {
            var nextAddress = await _dbContext.UserAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextAddress is null) return;
            
            nextAddress.SetDefault(true);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SetDefaultAddressAsync(string addressId, string userId, CancellationToken cancellationToken = default)
    {
        var parsedAddressId = Guid.Parse(addressId);
        var address = await _dbContext.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == parsedAddressId && a.UserId == userId, cancellationToken);

        if (address is null)
            throw new KeyNotFoundException("Address not found.");

        if (address.IsDefault) return;

        await UnsetOtherDefaultsAsync(userId, cancellationToken);
        address.SetDefault(true);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UnsetOtherDefaultsAsync(string userId, CancellationToken cancellationToken)
    {
        var defaultAddresses = await _dbContext.UserAddresses
            .Where(a => a.UserId == userId && a.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var addr in defaultAddresses)
        {
            addr.SetDefault(false);
        }
    }
}
