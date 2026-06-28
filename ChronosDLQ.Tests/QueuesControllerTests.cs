using ChronosDLQ.App.Models;
using ChronosDLQ.App.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ChronosDLQ.Tests;

public class QueuesControllerTests
{
    private readonly Mock<IQueueDiscoveryService> _queueDiscoveryService = new();
    private readonly Mock<IQueueWatchService> _queueWatchService = new();

    [Fact]
    public async Task GetQueues_ShouldReturnDiscoveredQueues()
    {
        // arrange
        var queues = new List<RabbitMqQueueInfo>
        {
            new("orders", 10, 10, 10, "running"),
            new("orders.dlq", 3, 0, 3, "running"),
        };

        _queueDiscoveryService
            .Setup(service => service.GetQueuesAsync(TestContext.Current.CancellationToken))
            .ReturnsAsync(queues);

        var controller = new QueuesController(
            _queueDiscoveryService.Object,
            _queueWatchService.Object
        );

        //act
        var response = await controller.GetQueues(TestContext.Current.CancellationToken);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);

        var returnedQueues = Assert.IsAssignableFrom<IEnumerable<RabbitMqQueueInfo>>(
            okResult.Value
        );

        //assert
        Assert.Equal(2, returnedQueues.Count());
    }

    [Fact]
    public void GetWatchedQueues_ShouldReturnWatchedQueues()
    {
        // arrange
        _queueWatchService
            .Setup(service => service.GetWatchedQueues())
            .Returns(["orders.dlq", "payments.dlq"]);

        var controller = new QueuesController(
            _queueDiscoveryService.Object,
            _queueWatchService.Object
        );

        //act
        var response = controller.GetWatchedQueues();

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var watchedQueues = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);

        //assert
        Assert.Contains("orders.dlq", watchedQueues);
        Assert.Contains("payments.dlq", watchedQueues);
    }

    [Fact]
    public async Task WatchQueue_ShouldReturnBadRequest_WhenQueueNameIsEmpty()
    {
        //arrange
        var controller = new QueuesController(
            _queueDiscoveryService.Object,
            _queueWatchService.Object
        );

        //act
        var response = await controller.WatchQueue(
            new WatchQueueRequest(""),
            TestContext.Current.CancellationToken
        );

        //assert
        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task WatchQueue_ShouldStartWatchingQueue_WhenQueueNameIsValid()
    {
        //arrange
        var controller = new QueuesController(
            _queueDiscoveryService.Object,
            _queueWatchService.Object
        );

        //act
        var response = await controller.WatchQueue(
            new WatchQueueRequest("orders.dlq"),
            TestContext.Current.CancellationToken
        );

        //assert
        Assert.IsType<OkObjectResult>(response);

        _queueWatchService.Verify(
            service => service.WatchQueueAsync("orders.dlq", TestContext.Current.CancellationToken),
            Times.Once
        );
    }

    [Fact]
    public async Task UnwatchQueue_ShouldStopWatchingQueue()
    {
        //arrange
        var controller = new QueuesController(
            _queueDiscoveryService.Object,
            _queueWatchService.Object
        );

        //act
        var response = await controller.UnwatchQueue(
            "orders.dlq",
            TestContext.Current.CancellationToken
        );

        Assert.IsType<OkObjectResult>(response);

        //assert
        _queueWatchService.Verify(
            service =>
                service.UnwatchQueueAsync("orders.dlq", TestContext.Current.CancellationToken),
            Times.Once
        );
    }
}
