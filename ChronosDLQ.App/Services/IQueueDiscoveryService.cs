using ChronosDLQ.App.Models;

namespace ChronosDLQ.App.Services;

public interface IQueueDiscoveryService
{
    Task<IReadOnlyCollection<RabbitMqQueueInfo>> GetQueuesAsync(
        CancellationToken cancellationToken
    );
}
