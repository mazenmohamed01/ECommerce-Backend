using ECommerce.Application.Contracts;
using ECommerce.Application.Features.Categories.Queries.GetActiveCategories;
using ECommerce.Application.Features.Categories.Queries.GetCategoryBySlug;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Public read-only category endpoints — no authentication required.
/// </summary>
[AllowAnonymous]
public sealed class CategoriesController : BaseApiController
{
    /// <summary>Returns all active categories ordered by SortOrder.</summary>
    /// <response code="200">List of active categories.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetActiveCategoriesQuery(), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>Returns a single active category by its URL slug.</summary>
    /// <param name="slug">The URL-friendly slug of the category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The requested category.</response>
    /// <response code="404">Category not found or inactive.</response>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug([FromRoute] string slug, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCategoryBySlugQuery(slug), cancellationToken);
        return HandleResult(result);
    }
}
