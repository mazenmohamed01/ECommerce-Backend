using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Common.Errors;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Features.UserAddresses.Commands.DeleteAddress;

internal sealed class DeleteAddressCommandHandler : ICommandHandler<DeleteAddressCommand, Result>
{
    private readonly IUserAddressRepository _userAddressRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAddressCommandHandler(IUserAddressRepository userAddressRepository, IUnitOfWork unitOfWork)
    {
        _userAddressRepository = userAddressRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteAddressCommand request, CancellationToken cancellationToken)
    {
        var address = await _userAddressRepository.GetUserAddressByIdAsync(request.AddressId, request.UserId, cancellationToken);

        if (address is null)
            return Result.Failure(DomainErrors.Address.NotFound);

        await _userAddressRepository.DeleteAsync(address, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (address.IsDefault)
        {
            var nextAddress = await _userAddressRepository.GetLatestAddressAsync(request.UserId, cancellationToken);
            if (nextAddress is not null)
            {
                nextAddress.SetDefault(true);
                await _userAddressRepository.UpdateAsync(nextAddress, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        return Result.Success();
    }
}
