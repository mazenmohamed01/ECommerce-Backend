using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using System;

namespace ECommerce.Application.Features.Categories.Commands.UploadCategoryImage;

public sealed record UploadCategoryImageCommand(Guid CategoryId, UploadCategoryImageRequest Request) : ICommand<Result<CategoryImageResponse>>;
