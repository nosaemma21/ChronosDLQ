using System.Text;
using ChronosDLQ.App.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ChronosDLQ.App.Health;

public class RabbitMqManagementHealthCheck : IHealthCheck
{
    private readonly IRabbitMqConnectionSettingsProvider _settingsProvider;
    private readonly IHttpClientFactory _httpClientFactory;

    public RabbitMqManagementHealthCheck(
        IRabbitMqConnectionSettingsProvider settingsProvider,
        IHttpClientFactory httpClientFactory
    )
    {
        _settingsProvider = settingsProvider;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var settings = await _settingsProvider.GetSettingsAsync(cancellationToken);
        if (settings is null)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ connection has not been configured.");
        }
        var baseUrl = settings.ManagementBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return HealthCheckResult.Unhealthy("RabbitMQ Management API base url missing");
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{baseUrl.TrimEnd('/')}/api/overview"
            );

            var authValue = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{settings.UserName}:{settings.Password}")
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
