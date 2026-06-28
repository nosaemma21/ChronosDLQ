using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using ChronosDLQ.App.Services;

namespace ChronosDLQ.app.Health;

public class RabbitMqAmqHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public RabbitMqAmqHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var factory = RabbitMqConnectionSettings
                .FromConfiguration(_configuration)
                .CreateConnectionFactory();

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
