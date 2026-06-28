namespace ChronosDLQ.App.Services;

public interface IRabbitMqConnectionSettingsProvider
{
    Task<RabbitMqConnectionSettings?> GetSettingsAsync(CancellationToken cancellationToken = default);
}
