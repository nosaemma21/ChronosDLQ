using System.Net;
using System.Net.Http.Json;
using ChronosDLQ.App.Models;
using ChronosDLQ.App.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ChronosDLQ.Tests;

public class MessagesApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly IMessageIndexStore _store;
    private readonly Mock<IMessageReplayService> _mockReplayService = new();
    private readonly WebApplicationFactory<Program> _factory;

    public MessagesApiTests(WebApplicationFactory<Program> factory)
    {
        // in-memory client to live api
        _client = factory.CreateClient();

        //  single instance of store
        _store = factory.Services.GetRequiredService<IMessageIndexStore>();

        _factory = factory;
    }

    [Fact]
    public async Task GetAllMessages_ShouldReturnListContainingPoisonMessage_WhenStoreIsSeeded()
    {
        var messageId = "err-999";
        var dummyMessage = new DeadLetterMessage
        {
            MessageId = messageId,
            ExceptionMessage = "NullReferenceException in processing pipeline",
            Timestamp = DateTime.UtcNow,
        };
        _store.AddOrUpdate(dummyMessage);

        var response = await _client.GetAsync(
            "/api/messages",
            TestContext.Current.CancellationToken
        );

        response.EnsureSuccessStatusCode();
        var messages = await response.Content.ReadFromJsonAsync<List<DeadLetterMessage>>(
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(messages);
        Assert.Contains(messages, m => m.MessageId == messageId);
    }

    [Fact]
    public async Task DiscardMessage_ShouldReturnokAndEvictMessage_WhenDeleteRouteIsExecuted()
    {
        // Ensuring a message exists
        var messageId = "delete-me-123";
        _store.AddOrUpdate(new DeadLetterMessage { MessageId = messageId });

        // Killing itttt
        var response = await _client.DeleteAsync(
            $"/api/messages/{messageId}",
            TestContext.Current.CancellationToken
        );

        // check for deletion
        var storeCheck = _store.GetById(messageId);

        //Asserting
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(storeCheck);
    }

    [Fact]
    public async Task DiscardMessage_ShouldReturn404NotFound_WhenMessageIdDoesNotExist()
    {
        // rando messageId url param to test
        var response = await _client.DeleteAsync(
            "api/messages/will-not-exist-surely",
            TestContext.Current.CancellationToken
        );

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReplayMessage_ShouldInvokeReplayServiceAndReturnOk_WhenExecutionSucceeds()
    {
        // Arrange
        var targetId = "dum-dum-id";
        var destinationQueue = "orders";
        var payloadModification = "{\"status\": \"replayed\"}";

        var mockedReplayService = new Mock<IMessageReplayService>();

        //mock
        mockedReplayService
            .Setup(s => s.ReplayMessageAsync(targetId, destinationQueue, payloadModification))
            .ReturnsAsync(true);

        // Isolated servr
        var isolatedClient = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(IMessageReplayService)
                    );
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddSingleton<IMessageReplayService>(mockedReplayService.Object);
                });
            })
            .CreateClient();

        var requestBody = new
        {
            MessageId = targetId,
            TargetQueue = destinationQueue,
            ModifiedPayload = payloadModification,
        };

        // Act
        var response = await isolatedClient.PostAsJsonAsync(
            "/api/messages/replay",
            requestBody,
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        mockedReplayService.Verify(
            s => s.ReplayMessageAsync(targetId, destinationQueue, payloadModification),
            Times.Once
        );
    }

    [Fact]
    public async Task ReplayMessage_ShouldReturn404NotFound_WhenMessageIdIsMissingFromStore()
    {
        // Arrange
        var missingId = "already-deleted-id";
        var mockedReplayService = new Mock<IMessageReplayService>();

        // Mock
        mockedReplayService
            .Setup(s => s.ReplayMessageAsync(missingId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var isolatedClient = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(IMessageReplayService)
                    );
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddSingleton<IMessageReplayService>(mockedReplayService.Object);
                });
            })
            .CreateClient();

        var requestBody = new
        {
            MessageId = missingId,
            TargetQueue = "orders",
            ModifiedPayload = "{}",
        };

        // Act
        var response = await isolatedClient.PostAsJsonAsync(
            "/api/messages/replay",
            requestBody,
            TestContext.Current.CancellationToken
        );

        // Asser
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReplayMessage_ShouldReturn400BadRequest_WhenPayloadHasSyntaxErrors()
    {
        // Arrange
        var malformedPayload = "\"orderId\": 1050 }";

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/messages/replay",
            new
            {
                MessageId = "msg-123",
                TargetQueue = "orders",
                ModifiedPayload = malformedPayload,
            },
            TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
