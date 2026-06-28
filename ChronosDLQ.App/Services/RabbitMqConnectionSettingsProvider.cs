namespace ChronosDLQ.App.Services;

public class RabbitMqConnectionSettingsProvider : IRabbitMqConnectionSettingsProvider
{
    private const string RabbitMqUrlHeader = "X-CHRONOS-RABBITMQ-URL";
    private const string RabbitMqManagementUrlHeader = "X-CHRONOS-RABBITMQ-MANAGEMENT-URL";

    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RabbitMqConnectionSettingsProvider(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<RabbitMqConnectionSettings?> GetSettingsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        var requestConnectionUrl = request?.Headers[RabbitMqUrlHeader].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(requestConnectionUrl))
        {
            var requestManagementUrl = request
                ?.Headers[RabbitMqManagementUrlHeader]
                .FirstOrDefault();

            return Task.FromResult<RabbitMqConnectionSettings?>(
                RabbitMqConnectionSettings.FromConnectionUrl(
                    requestConnectionUrl,
                    requestManagementUrl
                )
            );
        }

        var settings = RabbitMqConnectionSettings.HasConfiguration(_configuration)
            ? RabbitMqConnectionSettings.FromConfiguration(_configuration)
            : null;

        return Task.FromResult(settings);
    }
}
