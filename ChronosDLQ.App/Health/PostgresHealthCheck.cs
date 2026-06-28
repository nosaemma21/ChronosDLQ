using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace ChronosDLQ.App.Health;

public class PostgresHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public PostgresHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var connectionString = _configuration.GetConnectionString("ChronosDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy("ChronosDb connection string is missing.");
        }
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("Postgres is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Postgres is unreachable.", ex);
        }
    }
}
