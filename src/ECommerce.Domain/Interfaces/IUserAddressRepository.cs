using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

public interface IUserAddressRepository : IRepository<UserAddress>
{
    Task<IEnumerable<UserAddress>> GetUserAddressesAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserAddress?> GetUserAddressByIdAsync(Guid addressId, string userId, CancellationToken cancellationToken = default);
    Task<bool> HasAddressesAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserAddress>> GetDefaultAddressesAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserAddress?> GetLatestAddressAsync(string userId, CancellationToken cancellationToken = default);
}
