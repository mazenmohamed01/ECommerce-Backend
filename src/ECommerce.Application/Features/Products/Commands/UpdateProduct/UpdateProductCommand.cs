using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using System;

namespace ECommerce.Application.Features.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(Guid Id, UpdateProductRequest Request) : ICommand<Result<ProductDetailResponse>>;
