using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(CreateCategoryRequest Request) : ICommand<Result<CategoryResponse>>;
