using ECommerce.Application.Contracts.Addresses;

namespace ECommerce.Application.Interfaces;

public interface IUserAddressService
{
    Task<IEnumerable<AddressDto>> GetUserAddressesAsync(string userId, CancellationToken cancellationToken = default);
    Task<AddressDto> GetAddressByIdAsync(string addressId, string userId, CancellationToken cancellationToken = default);
    Task<AddressDto> CreateAddressAsync(string userId, CreateAddressRequest request, CancellationToken cancellationToken = default);
    Task<AddressDto> UpdateAddressAsync(string addressId, string userId, UpdateAddressRequest request, CancellationToken cancellationToken = default);
    Task DeleteAddressAsync(string addressId, string userId, CancellationToken cancellationToken = default);
    Task SetDefaultAddressAsync(string addressId, string userId, CancellationToken cancellationToken = default);
}
