using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChronosDLQ.App.Models;

namespace ChronosDLQ.App.Services;

public class RabbitMqManagementQueueDiscovery : IQueueDiscoveryService
{
    private readonly HttpClient _httpClient;
    private readonly IRabbitMqConnectionSettingsProvider _settingsProvider;
    private readonly ILogger<RabbitMqManagementQueueDiscovery> _logger;

    public RabbitMqManagementQueueDiscovery(
        HttpClient httpClient,
        IRabbitMqConnectionSettingsProvider settingsProvider,
        ILogger<RabbitMqManagementQueueDiscovery> logger
    )
    {
        _httpClient = httpClient;
        _settingsProvider = settingsProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<RabbitMqQueueInfo>> GetQueuesAsync(
        CancellationToken cancellationToken
    )
    {
        var settings = await _settingsProvider.GetSettingsAsync(cancellationToken);
        if (settings is null)
        {
            throw new InvalidOperationException("RabbitMQ connection has not been configured.");
        }
        var baseUrl =
            settings.ManagementBaseUrl
            ?? throw new InvalidOperationException(
                "RabbitMQ Management API base URL is not configured."
            );
        var encodedVhost = Uri.EscapeDataString(settings.VirtualHost);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl.TrimEnd('/')}/api/queues/{encodedVhost}"
        );

        var authValue = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{settings.UserName}:{settings.Password}")
        );
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "RabbitMQ queue discovery failed with status code {StatusCode}",
                response.StatusCode
            );
            response.EnsureSuccessStatusCode();
        }

        var queues =
            await response.Content.ReadFromJsonAsync<List<RabbitMqQueueDto>>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                cancellationToken
            ) ?? [];

        return queues
            .Select(queue => new RabbitMqQueueInfo(
                queue.Name,
                queue.MessagesReady,
                queue.MessagesUnacknowledged,
                queue.Messages,
                queue.State
            ))
            .OrderBy(queue => queue.Name)
            .ToArray();
    }

    private sealed record RabbitMqQueueDto
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("messages_ready")]
        public int MessagesReady { get; init; }

        [JsonPropertyName("messages_unacknowledged")]
        public int MessagesUnacknowledged { get; init; }

        [JsonPropertyName("messages")]
        public int Messages { get; init; }

        [JsonPropertyName("state")]
        public string State { get; init; } = string.Empty;
    }
}
