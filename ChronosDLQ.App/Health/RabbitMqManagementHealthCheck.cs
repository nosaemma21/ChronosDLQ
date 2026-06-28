using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ChronosDLQ.App.Health;

public class RabbitMqManagementHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public RabbitMqManagementHealthCheck(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory
    )
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        //   throw new NotImplementedException();
        var baseUrl = _configuration["RabbitMq:ManagementBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return HealthCheckResult.Unhealthy("RabbitMQ Management API base url missing");
        }

        try
        {
            var userName = _configuration["RabbitMq:UserName"] ?? "guest";
            var password = _configuration["RabbitMq:Password"] ?? "guest";

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{baseUrl.TrimEnd('/')}/api/overview"
            );

            var authValue = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{userName}:{password}")
            );

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic",
                authValue
            );

            var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.SendAsync(request, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("RabbitMQ Management API reachable")
                : HealthCheckResult.Unhealthy(
                    $"RabbitMQ Management API returned {(int)response.StatusCode}."
                );
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ Management API is unreachable.", ex);
        }
    }
}
