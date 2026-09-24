using ECommerce.Application.Contracts.Addresses;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Only authenticated users can manage their addresses
public class AddressesController : ControllerBase
{
    private readonly IUserAddressService _userAddressService;

    public AddressesController(IUserAddressService userAddressService)
    {
        _userAddressService = userAddressService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AddressDto>>> GetUserAddresses(CancellationToken cancellationToken)
    {
        var addresses = await _userAddressService.GetUserAddressesAsync(UserId, cancellationToken);
        return Ok(addresses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AddressDto>> GetAddressById(string id, CancellationToken cancellationToken)
    {
        try
        {
            var address = await _userAddressService.GetAddressByIdAsync(id, UserId, cancellationToken);
            return Ok(address);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<ActionResult<AddressDto>> CreateAddress([FromBody] CreateAddressRequest request, CancellationToken cancellationToken)
    {
        var address = await _userAddressService.CreateAddressAsync(UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetAddressById), new { id = address.Id }, address);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AddressDto>> UpdateAddress(string id, [FromBody] UpdateAddressRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var address = await _userAddressService.UpdateAddressAsync(id, UserId, request, cancellationToken);
            return Ok(address);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAddress(string id, CancellationToken cancellationToken)
    {
        try
        {
            await _userAddressService.DeleteAddressAsync(id, UserId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id}/set-default")]
    public async Task<ActionResult> SetDefaultAddress(string id, CancellationToken cancellationToken)
    {
        try
        {
            await _userAddressService.SetDefaultAddressAsync(id, UserId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
