using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using System;

namespace ECommerce.Application.Features.Products.Commands.UploadProductImage;

public sealed record UploadProductImageCommand(Guid ProductId, UploadProductImageRequest Request) : ICommand<Result<ProductImageResponse>>;
