namespace ChronosDLQ.App.Models;

/// <summary>
/// Represents a failed message extracted from the message brokers DLQ
/// </summary>
public class DeadLetterMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
    public string RawPayload { get; set; } = string.Empty;
    public string ExceptionMessage { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
