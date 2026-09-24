using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Interfaces;

public interface IAdminDashboardService
{
    Task<Result<DashboardStatsResponse>> GetDashboardStatsAsync(CancellationToken cancellationToken = default);
}
