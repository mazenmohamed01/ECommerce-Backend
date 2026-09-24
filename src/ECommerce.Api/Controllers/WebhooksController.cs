using ECommerce.Application.Contracts;
using ECommerce.Application.Features.Webhooks.Commands.ProcessMoyasarWebhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[Route("api/webhooks")]
[AllowAnonymous]
public sealed class WebhooksController : BaseApiController
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IConfiguration configuration,
        ILogger<WebhooksController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Endpoint for receiving Moyasar payment status webhooks.</summary>
    [HttpPost("moyasar")]
    public async Task<IActionResult> HandleMoyasarWebhook(
        [FromBody] MoyasarWebhookPayload payload,
        CancellationToken cancellationToken)
    {
        var configuredSecret = _configuration["Moyasar:WebhookSecret"];
        
        if (string.IsNullOrEmpty(configuredSecret))
        {
            _logger.LogError("Moyasar WebhookSecret is not configured.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Server configuration error." });
        }

        var result = await Sender.Send(new ProcessMoyasarWebhookCommand(payload, configuredSecret), cancellationToken);

        return HandleResult(result);
    }
}
