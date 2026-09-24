using ECommerce.Application.Contracts.Profile;
using ECommerce.Application.Features.Profiles.Queries.GetProfile;
using ECommerce.Application.Features.Profiles.Commands.UpdateProfile;
using ECommerce.Application.Features.Profiles.Commands.ChangePassword;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : BaseApiController
{

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCustomerProfileQuery(UserId), cancellationToken);
        return HandleResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateProfileCommand(UserId, request), cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ChangePasswordCommand(UserId, request), cancellationToken);
        return HandleResult(result);
    }
}
