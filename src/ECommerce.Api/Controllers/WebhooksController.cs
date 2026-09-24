using ECommerce.Api.Extensions;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class WebhooksController : ControllerBase
{
    private readonly IMoyasarWebhookService _webhookService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IMoyasarWebhookService webhookService,
        IConfiguration configuration,
        ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
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

        var result = await _webhookService.ProcessWebhookAsync(payload, configuredSecret, cancellationToken);

        // Uses standard ApiControllerExtensions to return 200 OK or appropriate error codes
        return this.Match(result);
    }
}
