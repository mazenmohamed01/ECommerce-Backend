using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Common.Errors;
using ECommerce.Application.Contracts.Addresses;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Features.UserAddresses.Commands.UpdateAddress;

internal sealed class UpdateAddressCommandHandler : ICommandHandler<UpdateAddressCommand, Result<AddressDto>>
{
    private readonly IUserAddressRepository _userAddressRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAddressCommandHandler(IUserAddressRepository userAddressRepository, IUnitOfWork unitOfWork)
    {
        _userAddressRepository = userAddressRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AddressDto>> Handle(UpdateAddressCommand request, CancellationToken cancellationToken)
    {
        var address = await _userAddressRepository.GetUserAddressByIdAsync(request.AddressId, request.UserId, cancellationToken);

        if (address is null)
            return Result.Failure<AddressDto>(DomainErrors.Address.NotFound);

        if (request.Request.IsDefault && !address.IsDefault)
        {
            var defaultAddresses = await _userAddressRepository.GetDefaultAddressesAsync(request.UserId, cancellationToken);
            foreach (var addr in defaultAddresses)
            {
                addr.SetDefault(false);
            }
            address.SetDefault(true);
        }
        else if (!request.Request.IsDefault && address.IsDefault)
        {
            address.SetDefault(false);
        }

        address.Update(
            request.Request.Title,
            request.Request.Street,
            request.Request.District,
            request.Request.City,
            request.Request.State,
            request.Request.PostalCode,
            request.Request.Country
        );

        await _userAddressRepository.UpdateAsync(address, cancellationToken);
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
