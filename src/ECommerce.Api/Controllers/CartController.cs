using System.Security.Claims;
using ECommerce.Application.Contracts;
using ECommerce.Application.Features.Carts.Commands.AddItemToCart;
using ECommerce.Application.Features.Carts.Commands.ClearCart;
using ECommerce.Application.Features.Carts.Commands.RemoveItemFromCart;
using ECommerce.Application.Features.Carts.Commands.UpdateCartItemQuantity;
using ECommerce.Application.Features.Carts.Queries.GetCart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Customer cart endpoints.
/// Strictly restricted to users with the Customer role.
/// </summary>
[Authorize(Roles = ECommerce.Domain.Constants.Roles.Customer)]
public sealed class CartController : BaseApiController
{
    private string GetCustomerId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Customer ID missing from token.");
    }

    /// <summary>
    /// Gets the authenticated customer's cart, calculating live product prices and stock availability.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The current cart state.</response>
    [HttpGet]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCartQuery(GetCustomerId()), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Adds a product to the cart or increments its quantity if it already exists.
    /// Creates a cart if the customer doesn't have one yet.
    /// </summary>
    /// <param name="request">The product and quantity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The updated cart state.</response>
    /// <response code="400">Invalid request (e.g. invalid quantity, out of stock).</response>
    /// <response code="404">Product not found.</response>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(
        [FromBody] AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new AddItemToCartCommand(GetCustomerId(), request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Updates the quantity of a specific cart item.
    /// Setting quantity to 0 removes the item.
    /// </summary>
    /// <param name="id">The unique identifier of the cart item.</param>
    /// <param name="request">The new quantity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The updated cart state.</response>
    /// <response code="400">Invalid request (e.g. quantity exceeds stock).</response>
    /// <response code="404">Cart or item not found.</response>
    [HttpPut("items/{id:guid}")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItemQuantity(
        [FromRoute] Guid id,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateCartItemQuantityCommand(GetCustomerId(), id, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Removes a specific item from the cart entirely.
    /// </summary>
    /// <param name="id">The unique identifier of the cart item to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The updated cart state.</response>
    /// <response code="404">Cart or item not found.</response>
    [HttpDelete("items/{id:guid}")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RemoveItemFromCartCommand(GetCustomerId(), id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Empties the cart.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Cart successfully cleared.</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ClearCartCommand(GetCustomerId()), cancellationToken);
        
        if (result.IsSuccess)
            return NoContent();
            
        return HandleResult(result);
    }
}
