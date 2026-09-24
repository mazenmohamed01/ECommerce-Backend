using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/v1/admin/orders")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public AdminOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OrderSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchOrders([FromQuery] AdminOrderSearchRequest request, CancellationToken cancellationToken)
    {
        var result = await _orderService.AdminSearchOrdersAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetByIdAsync(id, customerId: null, cancellationToken);
        
        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("NotFound")) return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _orderService.UpdateOrderStatusAsync(id, request, adminId, cancellationToken);
        
        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("NotFound")) return NotFound(result.Error);
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }
}
