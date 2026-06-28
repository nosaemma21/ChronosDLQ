using ChronosDLQ.App.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChronosDLQ.Tests;

public class QueueWatchServiceTests
{
    private readonly Mock<IMessageBrokerConsumer> _brokerConsumer = new();
    private readonly Mock<ILogger<QueueWatchService>> _logger = new();

    [Fact]
    public async Task WatchQueueAsync_ShouldStartConsumerAndTrackQueue_WhenQueueIsNew()
    {
        // arrange
        var service = new QueueWatchService(_brokerConsumer.Object, _logger.Object);

        // act
        await service.WatchQueueAsync("orders.dlq", TestContext.Current.CancellationToken);

        var watchedQueues = service.GetWatchedQueues();

        Assert.Contains("orders.dlq", watchedQueues);

        _brokerConsumer.Verify(
            consumer =>
                consumer.StartConsumingAsync("orders.dlq", TestContext.Current.CancellationToken),
            Times.Once
        );
    }

    [Fact]
    public async Task WatchQueueAsync_ShouldTrimQueueNameBeforeTracking()
    {
        var service = new QueueWatchService(_brokerConsumer.Object, _logger.Object);

        await service.WatchQueueAsync("  orders.dlq  ", TestContext.Current.CancellationToken);

        var watchedQueues = service.GetWatchedQueues();

        Assert.Contains("orders.dlq", watchedQueues);
        Assert.DoesNotContain("  orders.dlq  ", watchedQueues);

        _brokerConsumer.Verify(
            consumer =>
                consumer.StartConsumingAsync("orders.dlq", TestContext.Current.CancellationToken),
            Times.Once
        );
    }

    [Fact]
    public async Task WatchQueueAsync_ShouldNotStartConsumerTwice_WhenQueueIsAlreadyWatched()
    {
        var service = new QueueWatchService(_brokerConsumer.Object, _logger.Object);

        await service.WatchQueueAsync("orders.dlq", TestContext.Current.CancellationToken);
        await service.WatchQueueAsync("orders.dlq", TestContext.Current.CancellationToken);

        _brokerConsumer.Verify(
            consumer =>
                consumer.StartConsumingAsync("orders.dlq", TestContext.Current.CancellationToken),
            Times.Once
        );
    }

    [Fact]
    public async Task UnwatchQueueAsync_ShouldStopConsumerAndRemoveQueue_WhenQueueIsWatched()
    {
        var service = new QueueWatchService(_brokerConsumer.Object, _logger.Object);

        await service.WatchQueueAsync("orders.dlq", TestContext.Current.CancellationToken);
        await service.UnwatchQueueAsync("orders.dlq", TestContext.Current.CancellationToken);

        var watchedQueues = service.GetWatchedQueues();

        Assert.DoesNotContain("orders.dlq", watchedQueues);

        _brokerConsumer.Verify(
            consumer =>
                consumer.StopConsumingAsync("orders.dlq", TestContext.Current.CancellationToken),
            Times.Once
        );
    }

    [Fact]
    public async Task UnwatchQueueAsync_ShouldNotStopConsumer_WhenQueueIsNotWatched()
    {
        var service = new QueueWatchService(_brokerConsumer.Object, _logger.Object);

        await service.UnwatchQueueAsync("missing.dlq", TestContext.Current.CancellationToken);

        _brokerConsumer.Verify(
            consumer =>
                consumer.StopConsumingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}
