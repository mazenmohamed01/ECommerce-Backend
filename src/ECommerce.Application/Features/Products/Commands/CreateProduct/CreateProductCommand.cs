using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(CreateProductRequest Request) : ICommand<Result<ProductDetailResponse>>;
