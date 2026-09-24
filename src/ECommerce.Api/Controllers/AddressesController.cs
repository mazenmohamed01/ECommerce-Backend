using ECommerce.Application.Contracts.Addresses;
using ECommerce.Application.Features.UserAddresses.Commands.CreateAddress;
using ECommerce.Application.Features.UserAddresses.Commands.DeleteAddress;
using ECommerce.Application.Features.UserAddresses.Commands.SetDefaultAddress;
using ECommerce.Application.Features.UserAddresses.Commands.UpdateAddress;
using ECommerce.Application.Features.UserAddresses.Queries.GetAddressById;
using ECommerce.Application.Features.UserAddresses.Queries.GetUserAddresses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.Api.Controllers;

[Authorize]
public class AddressesController : BaseApiController
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AddressDto>>> GetUserAddresses(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetUserAddressesQuery(UserId), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AddressDto>> GetAddressById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAddressByIdQuery(id, UserId), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<AddressDto>> CreateAddress([FromBody] CreateAddressRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CreateAddressCommand(UserId, request), cancellationToken);
        
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetAddressById), new { id = result.Value.Id }, result.Value);
        }

        return HandleResult(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AddressDto>> UpdateAddress(Guid id, [FromBody] UpdateAddressRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateAddressCommand(id, UserId, request), cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAddress(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeleteAddressCommand(id, UserId), cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id}/set-default")]
    public async Task<ActionResult> SetDefaultAddress(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new SetDefaultAddressCommand(id, UserId), cancellationToken);
        return HandleResult(result);
    }
}
