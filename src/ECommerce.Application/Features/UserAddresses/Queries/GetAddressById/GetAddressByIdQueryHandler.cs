using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Common.Errors;
using ECommerce.Application.Contracts.Addresses;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Features.UserAddresses.Queries.GetAddressById;

internal sealed class GetAddressByIdQueryHandler : IQueryHandler<GetAddressByIdQuery, Result<AddressDto>>
{
    private readonly IUserAddressRepository _userAddressRepository;

    public GetAddressByIdQueryHandler(IUserAddressRepository userAddressRepository)
    {
        _userAddressRepository = userAddressRepository;
    }

    public async Task<Result<AddressDto>> Handle(GetAddressByIdQuery request, CancellationToken cancellationToken)
    {
        var address = await _userAddressRepository.GetUserAddressByIdAsync(request.AddressId, request.UserId, cancellationToken);

        if (address is null)
            return Result.Failure<AddressDto>(DomainErrors.Address.NotFound);

        return Result.Success(new AddressDto(
            address.Id.ToString(),
            address.Title,
            address.Street,
            address.District,
            address.City,
            address.State,
            address.PostalCode,
            address.Country,
            address.IsDefault
        ));
    }
}
