using ECommerce.Application.Contracts;
using ECommerce.Application.Features.Orders.Commands.UpdateOrderStatus;
using ECommerce.Application.Features.Orders.Queries.AdminSearchOrders;
using ECommerce.Application.Features.Orders.Queries.GetOrderById;
using ECommerce.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.Api.Controllers;

[Route("api/v1/admin/orders")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminOrdersController : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OrderSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchOrders([FromQuery] AdminOrderSearchRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new AdminSearchOrdersQuery(request), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetOrderByIdQuery(id, null), cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await Sender.Send(new UpdateOrderStatusCommand(id, request, adminId), cancellationToken);
        return HandleResult(result);
    }
}
