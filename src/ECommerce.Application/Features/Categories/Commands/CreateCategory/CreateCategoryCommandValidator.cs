using FluentValidation;

namespace ECommerce.Application.Features.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    private const string SlugPattern = @"^[\p{L}\p{N}]+(?:-[\p{L}\p{N}]+)*$";

    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(150).WithMessage("Category name must not exceed 150 characters.");

        RuleFor(x => x.Request.Slug)
            .Matches(SlugPattern)
                .WithMessage("Slug must be lowercase alphanumeric with hyphens only (e.g. 'hair-care').")
            .MaximumLength(150)
                .WithMessage("Slug must not exceed 150 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Slug));

        RuleFor(x => x.Request.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Request.Description is not null);

        RuleFor(x => x.Request.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("SortOrder must be a non-negative integer.");
    }
}
