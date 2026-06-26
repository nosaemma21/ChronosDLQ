namespace ChronosDLQ.App.Services;

public interface IQueueWatchService
{
    IReadOnlyCollection<string> GetWatchedQueues();
    Task WatchQueueAsync(string queueName, CancellationToken cancellationToken);
    Task UnwatchQueueAsync(string queueName, CancellationToken cancellationToken);
}
