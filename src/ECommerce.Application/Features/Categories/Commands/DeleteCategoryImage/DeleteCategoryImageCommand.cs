using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using System;

namespace ECommerce.Application.Features.Categories.Commands.DeleteCategoryImage;

public sealed record DeleteCategoryImageCommand(Guid CategoryId) : ICommand<Result>;
