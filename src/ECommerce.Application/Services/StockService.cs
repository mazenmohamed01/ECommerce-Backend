using ECommerce.Application.Common;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public sealed class StockService : IStockService
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StockService> _logger;

    public StockService(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<StockService> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> ReserveStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation("InvalidQuantity", "Quantity must be greater than zero."));

        var product = await _productRepository.GetByIdForUpdateAsync(productId, cancellationToken);
        
        if (product == null)
        {
            return Result.Failure(Error.NotFound("ProductNotFound", $"Product {productId} not found."));
        }

        if (!product.IsActive)
        {
            return Result.Failure(Error.Validation("ProductInactive", $"Product {productId} is inactive."));
        }

        try
        {
            product.ReserveStock(quantity);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Conflict("InsufficientStock", ex.Message));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully reserved {Quantity} units for product {ProductId}.", quantity, productId);
        return Result.Success();
    }

    public async Task<Result> ReleaseStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation("InvalidQuantity", "Quantity must be greater than zero."));

        var product = await _productRepository.GetByIdForUpdateAsync(productId, cancellationToken);
        
        if (product == null)
        {
            return Result.Failure(Error.NotFound("ProductNotFound", $"Product {productId} not found."));
        }

        try
        {
            product.ReleaseReservation(quantity);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Conflict("InvalidRelease", ex.Message));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully released {Quantity} reserved units for product {ProductId}.", quantity, productId);
        return Result.Success();
    }

    public async Task<Result> ConfirmStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation("InvalidQuantity", "Quantity must be greater than zero."));

        try
        {
            // Atomically decrement both pools
            await _productRepository.UpdateStockAsync(productId, -quantity, -quantity, cancellationToken);
            
            _logger.LogInformation("Successfully confirmed and deducted {Quantity} units of stock for product {ProductId}.", quantity, productId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm stock for product {ProductId}", productId);
            return Result.Failure(Error.Failure("StockConfirmationFailed", "An unexpected error occurred confirming stock."));
        }
    }

    public async Task<Result> RestoreStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            return Result.Failure(Error.Validation("InvalidQuantity", "Quantity must be greater than zero."));

        try
        {
            // Restoring means adding to QuantityInStock, but NOT ReservedStock, 
            // since it's fully cancelled/refunded and available to sell again.
            await _productRepository.UpdateStockAsync(productId, quantity, 0, cancellationToken);
            
            _logger.LogInformation("Successfully restored {Quantity} units of stock for product {ProductId}.", quantity, productId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore stock for product {ProductId}", productId);
            return Result.Failure(Error.Failure("StockRestoreFailed", "An unexpected error occurred restoring stock."));
        }
    }
}
