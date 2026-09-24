using ECommerce.Api.Extensions;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Public read-only category endpoints — no authentication required.
/// </summary>
[ApiController]
[Route("api/categories")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
        => _categoryService = categoryService;

    /// <summary>Returns all active categories ordered by SortOrder.</summary>
    /// <response code="200">List of active categories.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetAllActiveAsync(cancellationToken);
        return this.Match(result);
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
        var result = await _categoryService.GetBySlugAsync(slug, cancellationToken);
        return this.Match(result);
    }
}
