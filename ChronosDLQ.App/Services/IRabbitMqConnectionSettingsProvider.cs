namespace ChronosDLQ.App.Services;

public interface IRabbitMqConnectionSettingsProvider
{
    Task<RabbitMqConnectionSettings?> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task<RabbitMqConnectionSettings> SaveAsync(
        string connectionUrl,
        string? managementBaseUrl,
        CancellationToken cancellationToken = default
    );
}
