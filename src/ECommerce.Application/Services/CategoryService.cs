using AutoMapper;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Application.Common;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

/// <summary>
/// Orchestrates category creation, update, soft-delete, and retrieval.
/// Enforces slug uniqueness and guards against deleting a category that still has products.
/// </summary>
public sealed class CategoryService : ICategoryService
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp"
    };
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngMagic  = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] WebPMagic = [0x52, 0x49, 0x46, 0x46]; // RIFF

    private const string CloudinaryFolder = "ecommerce/categories";

    private readonly ICategoryRepository    _categoryRepository;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ISlugService           _slugService;
    private readonly ICloudinaryService     _cloudinaryService;
    private readonly IMapper                _mapper;
    private readonly ILogger<CategoryService> _logger;
    private readonly IValidator<CreateCategoryRequest> _createValidator;
    private readonly IValidator<UpdateCategoryRequest> _updateValidator;
    private readonly IValidator<UploadCategoryImageRequest> _imageValidator;

    public CategoryService(
        ICategoryRepository               categoryRepository,
        IUnitOfWork                       unitOfWork,
        ISlugService                      slugService,
        ICloudinaryService                cloudinaryService,
        IMapper                           mapper,
        ILogger<CategoryService>          logger,
        IValidator<CreateCategoryRequest> createValidator,
        IValidator<UpdateCategoryRequest> updateValidator,
        IValidator<UploadCategoryImageRequest> imageValidator)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork         = unitOfWork;
        _slugService        = slugService;
        _cloudinaryService  = cloudinaryService;
        _mapper             = mapper;
        _logger             = logger;
        _createValidator    = createValidator;
        _updateValidator    = updateValidator;
        _imageValidator     = imageValidator;
    }

    /// <inheritdoc/>
    public async Task<Result<CategoryResponse>> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        // ── Validate ──────────────────────────────────────────────────────────
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<CategoryResponse>(Error.Validation(
                "VALIDATION_ERROR", 
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        // ── Resolve slug ──────────────────────────────────────────────────────
        string slug;

        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            // Client explicitly provided a slug — check uniqueness; do NOT auto-suffix
            var duplicate = await _categoryRepository.SlugExistsAsync(
                request.Slug, excludeId: null, cancellationToken);

            if (duplicate)
                return Result.Failure<CategoryResponse>(Error.Conflict(
                    "CATEGORY_SLUG_DUPLICATE", $"The slug '{request.Slug}' is already in use."));

            slug = request.Slug;
        }
        else
        {
            // Auto-generate and auto-suffix until unique
            var baseSlug = _slugService.GenerateSlug(request.Name);
            slug = await _slugService.EnsureUniqueSlugAsync(
                baseSlug,
                s => _categoryRepository.SlugExistsAsync(s, excludeId: null, cancellationToken),
                cancellationToken);
        }

        // ── Persist ───────────────────────────────────────────────────────────
        var category = Category.Create(
            request.Name,
            slug,
            request.Description,
            imageUrl: null,
            request.SortOrder);

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Category created: {CategoryId} slug={Slug}", category.Id, slug);

        return MapToResponse(category);
    }

    /// <inheritdoc/>
    public async Task<Result<CategoryResponse>> UpdateAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        // ── Validate ──────────────────────────────────────────────────────────
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<CategoryResponse>(Error.Validation(
                "VALIDATION_ERROR", 
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        // ── Load ──────────────────────────────────────────────────────────────
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
            return Result.Failure<CategoryResponse>(Error.NotFound(
                "CATEGORY_NOT_FOUND", $"Category with ID '{id}' was not found."));

        // ── Resolve slug ──────────────────────────────────────────────────────
        string slug;

        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            // Client explicitly provided a slug — check uniqueness excluding self
            var duplicate = await _categoryRepository.SlugExistsAsync(
                request.Slug, excludeId: id, cancellationToken);

            if (duplicate)
                return Result.Failure<CategoryResponse>(Error.Conflict(
                    "CATEGORY_SLUG_DUPLICATE", $"The slug '{request.Slug}' is already in use."));

            slug = request.Slug;
        }
        else
        {
            var baseSlug = _slugService.GenerateSlug(request.Name);
            slug = await _slugService.EnsureUniqueSlugAsync(
                baseSlug,
                s => _categoryRepository.SlugExistsAsync(s, excludeId: id, cancellationToken),
                cancellationToken);
        }

        // ── Apply changes ─────────────────────────────────────────────────────
        category.Update(
            request.Name,
            slug,
            request.Description,
            category.ImageUrl, // Keep existing image URL
            request.SortOrder,
            request.IsActive);

        await _categoryRepository.UpdateAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Category updated: {CategoryId}", id);

        return MapToResponse(category);
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
            return Result.Failure(Error.NotFound(
                "CATEGORY_NOT_FOUND", $"Category with ID '{id}' was not found."));

        await _categoryRepository.DeleteAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Category hard-deleted: {CategoryId}", id);
        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<CategoryResponse>>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllActiveAsync(cancellationToken);
        return Result.Success(categories.Select(MapToResponse));
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<CategoryResponse>>> GetAllForAdminAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllForAdminAsync(cancellationToken);
        return Result.Success(categories.Select(MapToResponse));
    }

    /// <inheritdoc/>
    public async Task<Result<CategoryResponse>> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetBySlugAsync(slug, cancellationToken);
        if (category is null)
            return Result.Failure<CategoryResponse>(Error.NotFound(
                "CATEGORY_NOT_FOUND", $"Category with slug '{slug}' was not found or is inactive."));

        return Result.Success(MapToResponse(category));
    }

    /// <inheritdoc/>
    public async Task<Result<CategoryImageResponse>> UploadImageAsync(
        Guid categoryId,
        UploadCategoryImageRequest request,
        CancellationToken cancellationToken = default)
    {
        // ── Structural validation ──────────────────────────────────────────────
        var validation = await _imageValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<CategoryImageResponse>(Error.Validation(
                "VALIDATION_ERROR",
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        // ── Load category ──────────────────────────────────────────────────────
        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
            return Result.Failure<CategoryImageResponse>(Error.NotFound(
                "CATEGORY_NOT_FOUND", $"Category with ID '{categoryId}' was not found."));

        // ── Validate file signature (magic bytes) ──────────────────────────────
        await using var stream = request.File.OpenReadStream();
        var signatureValid = await IsValidImageSignatureAsync(stream, cancellationToken);
        if (!signatureValid)
            return Result.Failure<CategoryImageResponse>(Error.Validation(
                "INVALID_IMAGE_FORMAT",
                "The uploaded file does not appear to be a valid JPEG, PNG, or WebP image."));

        // Reset stream for upload
        stream.Seek(0, SeekOrigin.Begin);

        // ── Delete old Cloudinary image if present (best-effort) ──────────────
        if (!string.IsNullOrWhiteSpace(category.ImagePublicId))
        {
            var deleted = await _cloudinaryService.DeleteImageAsync(
                category.ImagePublicId, cancellationToken);

            if (!deleted)
                _logger.LogWarning(
                    "Could not delete old Cloudinary image {PublicId} for category {CategoryId}",
                    category.ImagePublicId, categoryId);
        }

        // ── Upload to Cloudinary ───────────────────────────────────────────────
        var uploadResult = await _cloudinaryService.UploadImageAsync(
            stream,
            request.File.FileName,
            CloudinaryFolder,
            cancellationToken);

        if (uploadResult is null)
            return Result.Failure<CategoryImageResponse>(Error.BadGateway(
                "CLOUDINARY_UPLOAD_FAILED",
                "Image upload to Cloudinary failed. Please try again later."));

        // ── Persist ───────────────────────────────────────────────────────────
        category.UpdateImage(uploadResult.Value.SecureUrl, uploadResult.Value.PublicId);
        await _categoryRepository.UpdateAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Category image uploaded: {CategoryId} publicId={PublicId}",
            categoryId, uploadResult.Value.PublicId);

        return Result.Success(new CategoryImageResponse
        {
            ImageUrl = uploadResult.Value.SecureUrl,
            AltText  = request.AltText
        });
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteImageAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        // ── Load category ──────────────────────────────────────────────────────
        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
            return Result.Failure(Error.NotFound(
                "CATEGORY_NOT_FOUND", $"Category with ID '{categoryId}' was not found."));

        if (string.IsNullOrWhiteSpace(category.ImageUrl))
            return Result.Failure(Error.Validation(
                "CATEGORY_NO_IMAGE", "This category does not have an image to delete."));

        var publicId = category.ImagePublicId;

        // ── Clear DB first ─────────────────────────────────────────────────────
        category.RemoveImage();
        await _categoryRepository.UpdateAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ── Delete from Cloudinary (soft-fail) ────────────────────────────────
        if (!string.IsNullOrWhiteSpace(publicId))
        {
            var deleted = await _cloudinaryService.DeleteImageAsync(publicId, cancellationToken);
            if (!deleted)
                _logger.LogWarning(
                    "DB cleared but Cloudinary deletion failed for publicId={PublicId} (category {CategoryId})",
                    publicId, categoryId);
        }

        _logger.LogInformation("Category image deleted: {CategoryId}", categoryId);
        return Result.Success();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static CategoryResponse MapToResponse(Category c) => new()
    {
        Id           = c.Id,
        Name         = c.Name,
        Slug         = c.Slug,
        Description  = c.Description,
        ImageUrl     = c.ImageUrl,
        SortOrder    = c.SortOrder,
        IsActive     = c.IsActive,
        ProductCount = c.Products.Count,
        CreatedAt    = c.CreatedAt,
        UpdatedAt    = c.UpdatedAt
    };

    private static async Task<bool> IsValidImageSignatureAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var header = new byte[8];
        var read   = await stream.ReadAsync(header.AsMemory(0, 8), cancellationToken);

        if (read < 3) return false;

        if (header[0] == JpegMagic[0] && header[1] == JpegMagic[1] && header[2] == JpegMagic[2])
            return true;

        if (read >= 4 &&
            header[0] == PngMagic[0] && header[1] == PngMagic[1] &&
            header[2] == PngMagic[2] && header[3] == PngMagic[3])
            return true;

        if (read >= 4 &&
            header[0] == WebPMagic[0] && header[1] == WebPMagic[1] &&
            header[2] == WebPMagic[2] && header[3] == WebPMagic[3])
            return true;

        return false;
    }
}
