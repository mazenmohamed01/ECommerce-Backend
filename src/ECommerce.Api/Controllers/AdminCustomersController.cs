using ECommerce.Application.Contracts;
using ECommerce.Application.Features.AdminCustomers.Queries.SearchCustomers;
using ECommerce.Application.Features.AdminCustomers.Queries.GetCustomerDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/v1/admin/customers")]
[Authorize(Policy = "AdminOnly")]
public class AdminCustomersController : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new SearchCustomersQuery(search, page, pageSize), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CustomerDetailsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomer(string id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCustomerDetailsQuery(id), cancellationToken);
        return HandleResult(result);
    }
}
