using ECommerce.Application.Interfaces;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.BackgroundJobs;

/// <summary>
/// Hangfire background job to automatically expire "Pending" "Online" orders 
/// that haven't been paid within the configured 15-minute window.
/// </summary>
public sealed class OrderExpiryJob
{
    private readonly IOrderRepository _orderRepository;
    private readonly IStockService _stockService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderExpiryJob> _logger;

    public OrderExpiryJob(
        IOrderRepository orderRepository,
        IStockService stockService,
        IUnitOfWork unitOfWork,
        ILogger<OrderExpiryJob> logger)
    {
        _orderRepository = orderRepository;
        _stockService = stockService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Executes the expiry job. Called automatically by Hangfire on a cron schedule.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("OrderExpiryJob started.");

        // Any order older than 15 minutes
        var cutoff = DateTime.UtcNow.AddMinutes(-15);
        
        var expiredOrders = await _orderRepository.GetExpiredPendingOnlineOrdersAsync(cutoff, cancellationToken);
        
        if (!expiredOrders.Any())
        {
            _logger.LogInformation("No expired pending orders found.");
            return;
        }

        _logger.LogInformation("Found {Count} expired pending orders. Cancelling them...", expiredOrders.Count);

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                foreach (var order in expiredOrders)
                {
                    try
                    {
                        order.UpdateStatus(
                            OrderStatus.Cancelled, 
                            changedByUserId: "SYSTEM", 
                            reason: "Payment timeout limit exceeded (15 mins)");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cancel order {OrderId}", order.Id);
                        continue;
                    }
                    
                    foreach (var item in order.Items)
                    {
                        var releaseResult = await _stockService.ReleaseStockAsync(item.ProductId, item.Quantity, ct);
                        if (releaseResult.IsFailure)
                        {
                            _logger.LogError("Failed to release stock for item {ProductId} in Order {OrderId}", item.ProductId, order.Id);
                            throw new InvalidOperationException($"Stock release failed: {releaseResult.Error.Message}");
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);

            _logger.LogInformation("OrderExpiryJob completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrderExpiryJob failed during batch cancellation.");
            throw; // Let Hangfire retry
        }
    }
}
