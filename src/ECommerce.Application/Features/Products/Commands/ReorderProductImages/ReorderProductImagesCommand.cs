using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using System;
using System.Collections.Generic;

namespace ECommerce.Application.Features.Products.Commands.ReorderProductImages;

public sealed record ReorderProductImagesCommand(Guid ProductId, ReorderProductImagesRequest Request) : ICommand<Result<IEnumerable<ProductImageResponse>>>;
