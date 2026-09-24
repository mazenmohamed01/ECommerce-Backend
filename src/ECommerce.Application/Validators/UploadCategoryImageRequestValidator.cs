using ECommerce.Application.Contracts;
using FluentValidation;

namespace ECommerce.Application.Validators;

/// <summary>
/// Validates <see cref="UploadCategoryImageRequest"/> before Cloudinary upload is attempted.
/// File signature (magic bytes) is checked in the service layer after this validator passes.
/// </summary>
public sealed class UploadCategoryImageRequestValidator : AbstractValidator<UploadCategoryImageRequest>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public UploadCategoryImageRequestValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("An image file is required.");

        When(x => x.File is not null, () =>
        {
            RuleFor(x => x.File.Length)
                .GreaterThan(0).WithMessage("The uploaded file is empty.")
                .LessThanOrEqualTo(MaxFileSizeBytes)
                    .WithMessage($"File size must not exceed {MaxFileSizeBytes / 1024 / 1024} MB.");

            RuleFor(x => x.File.ContentType)
                .Must(ct => AllowedContentTypes.Contains(ct))
                    .WithMessage("Only JPEG, PNG, and WebP images are accepted.");
        });

        RuleFor(x => x.AltText)
            .MaximumLength(200).WithMessage("Alt text must not exceed 200 characters.")
            .When(x => x.AltText is not null);
    }
}
