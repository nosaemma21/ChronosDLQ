using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using ChronosDLQ.App.Services;

namespace ChronosDLQ.app.Health;

public class RabbitMqAmqHealthCheck : IHealthCheck
{
    private readonly IRabbitMqConnectionSettingsProvider _settingsProvider;

    public RabbitMqAmqHealthCheck(IRabbitMqConnectionSettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var settings = await _settingsProvider.GetSettingsAsync(cancellationToken);
            if (settings is null)
            {
                return HealthCheckResult.Unhealthy("RabbitMQ connection has not been configured.");
            }

            var factory = settings.CreateConnectionFactory();

            using var connection = await factory.CreateConnectionAsync(cancellationToken);

            return connection.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ AMQP reachable.")
                : HealthCheckResult.Unhealthy("RabbitMQ AMQP connection closed.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ AMQP unreachable", ex);
        }
    }
}
