using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using System;

namespace ECommerce.Application.Features.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(Guid Id, UpdateCategoryRequest Request) : ICommand<Result<CategoryResponse>>;
