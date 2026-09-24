using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using System;

namespace ECommerce.Application.Features.Products.Commands.DeleteProductImage;

public sealed record DeleteProductImageCommand(Guid ProductId, Guid ImageId) : ICommand<Result>;
