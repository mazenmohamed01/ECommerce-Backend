using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using System;

namespace ECommerce.Application.Features.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : ICommand<Result>;
