using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using System;

namespace ECommerce.Application.Features.Products.Commands.SetMainProductImage;

public sealed record SetMainProductImageCommand(Guid ProductId, Guid ImageId) : ICommand<Result<ProductImageResponse>>;
