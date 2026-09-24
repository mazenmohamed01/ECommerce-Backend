using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize(Roles = Roles.Customer)]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost("checkout")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Checkout([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        var result = await _orderService.CheckoutAsync(customerId, request, cancellationToken);
        
        if (result.IsFailure)
            return result.Error.Code == "Validation" ? BadRequest(result.Error) : StatusCode(500, result.Error);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OrderSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        var result = await _orderService.GetCustomerOrdersAsync(customerId, request, cancellationToken);
        
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        var result = await _orderService.GetByIdAsync(id, customerId, cancellationToken);
        
        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("NotFound")) return NotFound(result.Error);
            if (result.Error.Code.Contains("Forbidden")) return Forbid();
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/pay")]
    [EnableRateLimiting("orders-policy")]
    [ProducesResponseType(typeof(PaymentInitiationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pay(Guid id, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        var result = await _orderService.InitiatePaymentAsync(id, customerId, cancellationToken);
        
        if (result.IsFailure)
        {
            if (result.Error.Code.Contains("NotFound")) return NotFound(result.Error);
            if (result.Error.Code.Contains("Denied") || result.Error.Code.Contains("Unauthorized")) return Forbid();
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    private string GetCustomerId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
