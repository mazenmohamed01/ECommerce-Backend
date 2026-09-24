using ECommerce.Application.Contracts;
using ECommerce.Application.Features.Orders.Commands.CheckoutOrder;
using ECommerce.Application.Features.Orders.Commands.InitiateOrderPayment;
using ECommerce.Application.Features.Orders.Queries.GetCustomerOrders;
using ECommerce.Application.Features.Orders.Queries.GetOrderById;
using ECommerce.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace ECommerce.Api.Controllers;

[Route("api/v1/orders")]
[Authorize(Roles = Roles.Customer)]
public sealed class OrdersController : BaseApiController
{
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Checkout([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        var result = await Sender.Send(new CheckoutOrderCommand(customerId, request), cancellationToken);
        
        if (result.IsFailure)
            return HandleResult(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OrderSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders([FromQuery] PaginationRequest request, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        var result = await Sender.Send(new GetCustomerOrdersQuery(customerId, request), cancellationToken);
        
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var customerId = GetCustomerId();
        var result = await Sender.Send(new GetOrderByIdQuery(id, customerId), cancellationToken);
        
        return HandleResult(result);
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
        var result = await Sender.Send(new InitiateOrderPaymentCommand(id, customerId), cancellationToken);
        
        return HandleResult(result);
    }

    private string GetCustomerId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
