using ECommerce.Application.Contracts;
using ECommerce.Application.Features.AdminDashboard.Queries.GetDashboardStats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Policy = "AdminOnly")]
public class AdminDashboardController : BaseApiController
{
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetDashboardStatsQuery(), cancellationToken);
        return HandleResult(result);
    }
}
