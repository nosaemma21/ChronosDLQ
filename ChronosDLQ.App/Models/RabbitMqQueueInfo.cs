namespace ChronosDLQ.App.Models;

public record RabbitMqQueueInfo(
    string Name,
    int Ready,
    int Unacked,
    int Total,
    string State
);
