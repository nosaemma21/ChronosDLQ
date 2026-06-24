namespace ChronosDLQ.App.Models;

public record ReplayRequest(string MessageId, string TargetQueue, string ModifiedPayload);
