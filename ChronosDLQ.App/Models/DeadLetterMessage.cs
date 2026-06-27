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
    public string? CorrelationId { get; set; }
    public string? ContentType { get; set; }
    public string? ContentEncoding { get; set; }
    public string? Type { get; set; }
    public string? ReplyTo { get; set; }
    public string? Expiration { get; set; }
    public string? AppId { get; set; }
    public bool Persistent { get; set; }
    public byte Priority { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}
