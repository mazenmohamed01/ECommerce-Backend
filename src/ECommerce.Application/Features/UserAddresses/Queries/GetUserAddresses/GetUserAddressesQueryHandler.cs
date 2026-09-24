using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts.Addresses;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Features.UserAddresses.Queries.GetUserAddresses;

internal sealed class GetUserAddressesQueryHandler : IQueryHandler<GetUserAddressesQuery, Result<IEnumerable<AddressDto>>>
{
    private readonly IUserAddressRepository _userAddressRepository;

    public GetUserAddressesQueryHandler(IUserAddressRepository userAddressRepository)
    {
        _userAddressRepository = userAddressRepository;
    }

    public async Task<Result<IEnumerable<AddressDto>>> Handle(GetUserAddressesQuery request, CancellationToken cancellationToken)
    {
        var addresses = await _userAddressRepository.GetUserAddressesAsync(request.UserId, cancellationToken);
        
        var dtos = addresses.Select(a => new AddressDto(
            a.Id.ToString(),
            a.Title,
            a.Street,
            a.District,
            a.City,
            a.State,
            a.PostalCode,
            a.Country,
            a.IsDefault
        ));

        return Result.Success(dtos);
    }
}
