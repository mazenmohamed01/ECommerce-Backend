using ECommerce.Api.Extensions;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

/// <summary>
/// Admin-only category management endpoints.
/// All routes require a valid JWT with the Admin role.
/// </summary>
[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public sealed class AdminCategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public AdminCategoriesController(ICategoryService categoryService)
        => _categoryService = categoryService;

    /// <summary>Returns all categories including inactive ones.</summary>
    /// <response code="200">Full list of categories.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetAllForAdminAsync(cancellationToken);
        return this.Match(result);
    }

    /// <summary>Creates a new category. Slug auto-generated from Name when not provided.</summary>
    /// <response code="201">Category created successfully.</response>
    /// <response code="400">Validation error or duplicate slug.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.CreateAsync(request, cancellationToken);

        return this.Match(result, category => 
            CreatedAtAction(nameof(CategoriesController.GetBySlug), 
                            "Categories", 
                            new { slug = category.Slug }, 
                            category));
    }

    /// <summary>Updates an existing category by ID.</summary>
    /// <param name="id">Category GUID.</param>
    /// <param name="request">Update request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Category updated successfully.</response>
    /// <response code="400">Validation error or duplicate slug.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.UpdateAsync(id, request, cancellationToken);
        return this.Match(result);
    }

    /// <summary>Soft-deletes a category (sets IsActive = false).</summary>
    /// <param name="id">Category GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Category deactivated.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Category has products — deactivate products first.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.DeleteAsync(id, cancellationToken);
        return this.Match(result);
    }

    /// <summary>
    /// Uploads (or replaces) the hero image for a category.
    /// Accepts a multipart/form-data request with the image file.
    /// Old image is automatically deleted from Cloudinary before the new one is uploaded.
    /// </summary>
    /// <param name="id">Category GUID.</param>
    /// <param name="request">Upload image request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Image uploaded. Returns the Cloudinary URL.</response>
    /// <response code="400">Validation error or invalid image format.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="502">Cloudinary upload failed.</response>
    [HttpPost("{id:guid}/image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CategoryImageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> UploadImage(
        [FromRoute] Guid id,
        [FromForm] UploadCategoryImageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.UploadImageAsync(id, request, cancellationToken);
        return this.Match(result);
    }

    /// <summary>
    /// Deletes the hero image of a category from Cloudinary and clears it from the database.
    /// </summary>
    /// <param name="id">Category GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Image deleted successfully.</response>
    /// <response code="400">Category has no image.</response>
    /// <response code="404">Category not found.</response>
    [HttpDelete("{id:guid}/image")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.DeleteImageAsync(id, cancellationToken);
        return this.Match(result);
    }
}
