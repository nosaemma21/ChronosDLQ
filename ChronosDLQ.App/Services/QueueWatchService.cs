using System.Collections.Concurrent;

namespace ChronosDLQ.App.Services;

public class QueueWatchService : IQueueWatchService
{
    private readonly IMessageBrokerConsumer _brokerConsumer;
    private readonly ILogger<QueueWatchService> _logger;
    private readonly ConcurrentDictionary<string, byte> _watchedQueues = new();

    public QueueWatchService(
        IMessageBrokerConsumer brokerConsumer,
        ILogger<QueueWatchService> logger
    )
    {
        _brokerConsumer = brokerConsumer;
        _logger = logger;
    }

    public IReadOnlyCollection<string> GetWatchedQueues()
    {
        return _watchedQueues.Keys.OrderBy(queueName => queueName).ToArray();
    }

    public async Task WatchQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentException("Queue name is required.", nameof(queueName));

        var normalizedQueueName = queueName.Trim();
        if (!_watchedQueues.TryAdd(normalizedQueueName, 0))
            return;

        try
        {
            await _brokerConsumer.StartConsumingAsync(normalizedQueueName, cancellationToken);
            _logger.LogInformation("Chronos is now watching queue {QueueName}", normalizedQueueName);
        }
        catch
        {
            _watchedQueues.TryRemove(normalizedQueueName, out _);
            throw;
        }
    }

    public async Task UnwatchQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            return;

        var normalizedQueueName = queueName.Trim();
        if (!_watchedQueues.TryRemove(normalizedQueueName, out _))
            return;

        await _brokerConsumer.StopConsumingAsync(normalizedQueueName, cancellationToken);
        _logger.LogInformation("Chronos stopped watching queue {QueueName}", normalizedQueueName);
    }
}
