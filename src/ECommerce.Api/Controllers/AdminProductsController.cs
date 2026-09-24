using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Constants;
using ECommerce.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Admin-only product management endpoints.
/// All routes require a valid JWT with the Admin role.
/// </summary>
[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public sealed class AdminProductsController : ControllerBase
{
    private readonly IProductService      _productService;
    private readonly IProductImageService _imageService;

    public AdminProductsController(
        IProductService      productService,
        IProductImageService imageService)
    {
        _productService = productService;
        _imageService   = imageService;
    }

    // ── Product CRUD ──────────────────────────────────────────────────────────

    /// <summary>Returns full product detail by ID (includes inactive/soft-deleted products).</summary>
    /// <param name="id">Product GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Full product detail.</response>
    /// <response code="404">Product not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.GetByIdForAdminAsync(id, cancellationToken);
        return this.Match(result);
    }

    /// <summary>Creates a new product. Slug auto-generated from Name when not provided.</summary>
    /// <response code="201">Product created successfully.</response>
    /// <response code="400">Validation error, duplicate slug, or inactive/missing category.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _productService.CreateAsync(request, cancellationToken);

        return this.Match(result, product => 
            CreatedAtAction(nameof(GetById), 
                            new { id = product.Id }, 
                            product));
    }

    /// <summary>Updates an existing product by ID.</summary>
    /// <param name="id">Product GUID.</param>
    /// <param name="request">Update product request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Product updated successfully.</response>
    /// <response code="400">Validation error, duplicate slug, or inactive/missing category.</response>
    /// <response code="404">Product not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateAsync(id, request, cancellationToken);
        return this.Match(result);
    }

    /// <summary>
    /// Soft-deletes a product (IsActive = false, DeletedAt = now).
    /// The product remains in the database and is visible to Admin but hidden from public.
    /// </summary>
    /// <param name="id">Product GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Product soft-deleted.</response>
    /// <response code="404">Product not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.DeleteAsync(id, cancellationToken);
        return this.Match(result);
    }

    // ── Product Image Management ──────────────────────────────────────────────

    /// <summary>
    /// Uploads an image for the product.
    /// File is uploaded to Cloudinary first; DB row is created only on success.
    /// </summary>
    /// <param name="id">Product GUID (route param).</param>
    /// <param name="request">Upload image request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Image uploaded and persisted.</response>
    /// <response code="400">Invalid file type or size.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="502">Cloudinary upload failed.</response>
    [HttpPost("{id:guid}/images")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductImageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> UploadImage(
        Guid id,
        [FromForm] UploadProductImageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _imageService.UploadAsync(id, request, cancellationToken);

        return this.Match(result, image => 
            CreatedAtAction(nameof(GetById), 
                            new { id }, 
                            image));
    }

    /// <summary>Deletes a product image. Cloudinary asset is also removed (best-effort).</summary>
    /// <param name="id">Product GUID.</param>
    /// <param name="imageId">Image GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Image deleted.</response>
    /// <response code="404">Image not found or does not belong to this product.</response>
    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(
        Guid id,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var result = await _imageService.DeleteAsync(id, imageId, cancellationToken);
        return this.Match(result);
    }

    /// <summary>Sets the specified image as the product's main (hero) image.</summary>
    /// <param name="id">Product GUID.</param>
    /// <param name="imageId">Image GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Image is now the main image.</response>
    /// <response code="404">Image not found or does not belong to this product.</response>
    [HttpPut("{id:guid}/images/{imageId:guid}/set-main")]
    [ProducesResponseType(typeof(ProductImageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetMainImage(
        Guid id,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var result = await _imageService.SetMainAsync(id, imageId, cancellationToken);
        return this.Match(result);
    }

    /// <summary>
    /// Reorders images for a product by specifying a new DisplayOrder for each image.
    /// All imageIds must belong to the specified product.
    /// </summary>
    /// <param name="id">Product GUID.</param>
    /// <param name="request">Reorder images request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Updated list of images in new order.</response>
    /// <response code="400">One or more imageIds do not belong to this product.</response>
    [HttpPut("{id:guid}/images/reorder")]
    [ProducesResponseType(typeof(IEnumerable<ProductImageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReorderImages(
        Guid id,
        [FromBody] ReorderProductImagesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _imageService.ReorderAsync(id, request, cancellationToken);
        return this.Match(result);
    }
}
