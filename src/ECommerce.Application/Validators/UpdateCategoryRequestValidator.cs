using ECommerce.Application.Contracts;
using FluentValidation;

namespace ECommerce.Application.Validators;

/// <summary>
/// Validates <see cref="UpdateCategoryRequest"/> before it reaches the service layer.
/// Inherits the same field rules as Create; IsActive has no additional constraint.
/// </summary>
public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    private const string SlugPattern = @"^[\p{L}\p{N}]+(?:-[\p{L}\p{N}]+)*$";

    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(150).WithMessage("Category name must not exceed 150 characters.");

        RuleFor(x => x.Slug)
            .Matches(SlugPattern)
                .WithMessage("Slug must be lowercase alphanumeric with hyphens only (e.g. 'hair-care').")
            .MaximumLength(150)
                .WithMessage("Slug must not exceed 150 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("SortOrder must be a non-negative integer.");
    }
}
