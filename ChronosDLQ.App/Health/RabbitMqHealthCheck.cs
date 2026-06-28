using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

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
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
                UserName = _configuration["RabbitMq:UserName"] ?? "guest",
                Password = _configuration["RabbitMq:Password"] ?? "guest",
                VirtualHost = _configuration["RabbitMq:VirtualHost"] ?? "/",
            };

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
