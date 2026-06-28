namespace ChronosDLQ.App.Models;

public record RabbitMqConfigurationRequest(string ConnectionUrl, string? ManagementBaseUrl);

public record RabbitMqConfigurationResponse(
    bool IsConfigured,
    string? HostName,
    string? VirtualHost,
    string? ManagementBaseUrl,
    DateTime? UpdatedAtUtc
);
