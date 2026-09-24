using ECommerce.Application.Contracts;

namespace ECommerce.Application.Interfaces;

public interface IN8nIntegrationService
{
    Task SendOrderCreatedEventAsync(OrderResponse order, CancellationToken cancellationToken = default);
}
