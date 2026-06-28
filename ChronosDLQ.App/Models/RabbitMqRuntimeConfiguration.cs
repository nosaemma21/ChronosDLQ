namespace ChronosDLQ.App.Models;

public class RabbitMqRuntimeConfiguration
{
    public int Id { get; set; }
    public string ConnectionUrl { get; set; } = string.Empty;
    public string? ManagementBaseUrl { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
