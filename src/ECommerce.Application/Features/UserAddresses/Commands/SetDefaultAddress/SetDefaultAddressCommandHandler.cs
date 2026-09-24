using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Common.Errors;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Features.UserAddresses.Commands.SetDefaultAddress;

internal sealed class SetDefaultAddressCommandHandler : ICommandHandler<SetDefaultAddressCommand, Result>
{
    private readonly IUserAddressRepository _userAddressRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetDefaultAddressCommandHandler(IUserAddressRepository userAddressRepository, IUnitOfWork unitOfWork)
    {
        _userAddressRepository = userAddressRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetDefaultAddressCommand request, CancellationToken cancellationToken)
    {
        var address = await _userAddressRepository.GetUserAddressByIdAsync(request.AddressId, request.UserId, cancellationToken);

        if (address is null)
            return Result.Failure(DomainErrors.Address.NotFound);

        if (address.IsDefault) return Result.Success();

        var defaultAddresses = await _userAddressRepository.GetDefaultAddressesAsync(request.UserId, cancellationToken);
        foreach (var addr in defaultAddresses)
        {
            addr.SetDefault(false);
        }

        address.SetDefault(true);
        await _userAddressRepository.UpdateAsync(address, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
