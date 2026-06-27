using System.Text;
using RabbitMQ.Client;

namespace ChronosDLQ.App.Services;

public class MessageReplayService : IMessageReplayService
{
    private readonly IMessageIndexStore _indexStore;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MessageReplayService> _logger;

    public MessageReplayService(
        IMessageIndexStore indexStore,
        IConfiguration configuration,
        ILogger<MessageReplayService> logger
    )
    {
        _indexStore = indexStore;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> ReplayMessageAsync(
        string messageId,
        string targetQueue,
        string modifiedPayload
    )
    {
        //   Checking if message exists
        var existingMessage = _indexStore.GetById(messageId);
        if (existingMessage == null)
        {
            _logger.LogWarning(
                "Replay aborted. Message {MessageId} not found in store.",
                messageId
            );
            return false;
        }

        try
        {
            // open a temp channel to send corrected payload back to queue
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
            };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            // Ensuring target execution queue is provisioned
            await channel.QueueDeclareAsync(
                queue: targetQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var body = Encoding.UTF8.GetBytes(modifiedPayload);
            var properties = new BasicProperties
            {
                MessageId = messageId,
                CorrelationId = existingMessage.CorrelationId,
                ContentType = existingMessage.ContentType,
                Type = existingMessage.Type,
                ReplyTo = existingMessage.ReplyTo,
                Expiration = existingMessage.Expiration,
                AppId = existingMessage.AppId,
                Persistent = true, //durable on disk
                Priority = existingMessage.Priority,
                Headers = existingMessage.Headers.ToDictionary(
                    header => header.Key,
                    header => (object?)header.Value
                ),
            };

            _logger.LogInformation(
                "Replaying corrected payload for message {MessageId} into queue {TargetQueue}",
                messageId,
                targetQueue
            );

            // Firing the corrected package into the prod queue stream
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: targetQueue,
                mandatory: true,
                basicProperties: properties,
                body: body
            );

            // Remove item from dashboard store so it drops off UI tracking boards
            _indexStore.Remove(messageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to execute message replay wizard ops for {MessageId}",
                messageId
            );
            throw;
        }
    }
}
