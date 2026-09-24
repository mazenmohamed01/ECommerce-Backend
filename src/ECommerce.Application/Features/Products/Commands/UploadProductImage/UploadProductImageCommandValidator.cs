using FluentValidation;
using System.Collections.Generic;
using System.IO;

namespace ECommerce.Application.Features.Products.Commands.UploadProductImage;

public sealed class UploadProductImageCommandValidator : AbstractValidator<UploadProductImageCommand>
{
    private static readonly HashSet<string> AllowedExtensions = new(System.StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly HashSet<string> AllowedMimeTypes  = new(System.StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public UploadProductImageCommandValidator()
    {
        RuleFor(x => x.Request.File)
            .NotNull().WithMessage("An image file is required.")
            .Must(f => f.Length > 0).WithMessage("File cannot be empty.")
            .Must(f => f.Length <= MaxFileSize).WithMessage("File size must not exceed 5 MB.")
            .Must(f => AllowedMimeTypes.Contains(f.ContentType))
                .WithMessage("Unsupported file type. Only JPEG, PNG, and WebP are allowed.")
            .Must(f => AllowedExtensions.Contains(Path.GetExtension(f.FileName)))
                .WithMessage("Unsupported file extension. Only .jpg, .jpeg, .png, and .webp are allowed.");

        RuleFor(x => x.Request.AltText)
            .MaximumLength(100).WithMessage("Alt text must not exceed 100 characters.");
    }
}
