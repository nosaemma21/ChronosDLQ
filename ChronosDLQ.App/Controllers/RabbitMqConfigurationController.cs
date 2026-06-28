using ChronosDLQ.App.Models;
using ChronosDLQ.App.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChronosDLQ.App.Controllers;

[ApiController]
[Route("api/rabbitmq/configuration")]
public class RabbitMqConfigurationController : ControllerBase
{
    private readonly IRabbitMqConnectionSettingsProvider _settingsProvider;

    public RabbitMqConfigurationController(
        IRabbitMqConnectionSettingsProvider settingsProvider
    )
    {
        _settingsProvider = settingsProvider;
    }

    [HttpGet]
    public async Task<ActionResult<RabbitMqConfigurationResponse>> GetConfiguration(
        CancellationToken cancellationToken
    )
    {
        var settings = await _settingsProvider.GetSettingsAsync(cancellationToken);

        if (settings is null)
        {
            return Ok(
                new RabbitMqConfigurationResponse(false, null, null, null, null)
            );
        }

        return Ok(
            new RabbitMqConfigurationResponse(
                true,
                settings.HostName,
                settings.VirtualHost,
                settings.ManagementBaseUrl,
                null
            )
        );
    }

    [HttpPut]
    public async Task<ActionResult<RabbitMqConfigurationResponse>> SaveConfiguration(
        [FromBody] RabbitMqConfigurationRequest request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.ConnectionUrl))
        {
            return BadRequest(new { message = "RabbitMQ connection URL is required." });
        }

        RabbitMqConnectionSettings settings;
        try
        {
            settings = await _settingsProvider.SaveAsync(
                request.ConnectionUrl,
                request.ManagementBaseUrl,
                cancellationToken
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(
            new RabbitMqConfigurationResponse(
                true,
                settings.HostName,
                settings.VirtualHost,
                settings.ManagementBaseUrl,
                DateTime.UtcNow
            )
        );
    }
}
