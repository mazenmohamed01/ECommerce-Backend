using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/v1/admin/customers")]
[Authorize(Policy = "AdminOnly")]
public class AdminCustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public AdminCustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerService.SearchCustomersAsync(search, page, pageSize, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CustomerDetailsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomer(string id, CancellationToken cancellationToken)
    {
        var result = await _customerService.GetCustomerDetailsAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }
}
