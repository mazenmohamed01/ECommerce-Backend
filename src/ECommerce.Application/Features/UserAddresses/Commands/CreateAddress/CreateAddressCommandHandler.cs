using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts.Addresses;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Features.UserAddresses.Commands.CreateAddress;

internal sealed class CreateAddressCommandHandler : ICommandHandler<CreateAddressCommand, Result<AddressDto>>
{
    private readonly IUserAddressRepository _userAddressRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAddressCommandHandler(IUserAddressRepository userAddressRepository, IUnitOfWork unitOfWork)
    {
        _userAddressRepository = userAddressRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AddressDto>> Handle(CreateAddressCommand request, CancellationToken cancellationToken)
    {
        bool hasAddresses = await _userAddressRepository.HasAddressesAsync(request.UserId, cancellationToken);
        bool isDefault = request.Request.IsDefault || !hasAddresses; // Force default if it's the first address

        if (isDefault && hasAddresses)
        {
            var defaultAddresses = await _userAddressRepository.GetDefaultAddressesAsync(request.UserId, cancellationToken);
            foreach (var addr in defaultAddresses)
            {
                addr.SetDefault(false);
            }
        }

        var address = UserAddress.Create(
            request.UserId,
            request.Request.Title,
            request.Request.Street,
            request.Request.District,
            request.Request.City,
            request.Request.State,
            request.Request.PostalCode,
            request.Request.Country,
            isDefault
        );

        await _userAddressRepository.AddAsync(address, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
