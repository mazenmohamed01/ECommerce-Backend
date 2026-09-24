using ECommerce.Api.Extensions;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Public product browsing endpoints — no authentication required.
/// Only returns active, non-deleted products.
/// </summary>
[ApiController]
[Route("api/products")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService                    _productService;
    private readonly IValidator<ProductSearchRequest>   _searchValidator;

    public ProductsController(
        IProductService                   productService,
        IValidator<ProductSearchRequest>  searchValidator)
    {
        _productService  = productService;
        _searchValidator = searchValidator;
    }

    /// <summary>
    /// Searches and browses products with optional filtering, sorting, and pagination.
    /// Only active products are returned.
    /// </summary>
    /// <response code="200">Paginated product list.</response>
    /// <response code="400">Invalid search parameters (e.g. PriceFrom > PriceTo).</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProductListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] ProductSearchRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _searchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var result = await _productService.SearchAsync(request, adminView: false, cancellationToken);
        return this.Match(result);
    }

    /// <summary>Returns full product details by URL slug. Only active products are returned.</summary>
    /// <param name="slug">URL-friendly product slug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Full product detail including images.</response>
    /// <response code="404">Product not found or inactive.</response>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ProductDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(
        [FromRoute] string slug,
        CancellationToken cancellationToken)
    {
        var result = await _productService.GetBySlugAsync(slug, cancellationToken);
        return this.Match(result);
    }
}
