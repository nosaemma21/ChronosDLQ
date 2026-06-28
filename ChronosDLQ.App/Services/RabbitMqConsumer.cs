using System.Collections.Concurrent;
using System.Text;
using ChronosDLQ.App.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChronosDLQ.App.Services;

class RabbitMqConsumer : IMessageBrokerConsumer
{
    private readonly IMessageIndexStore _indexStore;
    private readonly IRabbitMqConnectionSettingsProvider _settingsProvider;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly ConcurrentDictionary<string, QueueConsumerHandle> _activeConsumers = new();
    private readonly SemaphoreSlim _consumerLock = new(1, 1);

    public RabbitMqConsumer(
        IMessageIndexStore indexStore,
        IRabbitMqConnectionSettingsProvider settingsProvider,
        ILogger<RabbitMqConsumer> logger
    )
    {
        _indexStore = indexStore;
        _settingsProvider = settingsProvider;
        _logger = logger;
    }

    public async Task StartConsumingAsync(string queueName, CancellationToken cancellationToken)
    {
        if (_activeConsumers.ContainsKey(queueName))
            return;

        await _consumerLock.WaitAsync(cancellationToken);
        try
        {
            if (_activeConsumers.ContainsKey(queueName))
                return;

            _logger.LogInformation(
                "Connecting to RabbitMQ broker for queue {QueueName}...",
                queueName
            );
            var settings = await _settingsProvider.GetSettingsAsync(cancellationToken);
            if (settings is null)
            {
                throw new InvalidOperationException("RabbitMQ connection has not been configured.");
            }

            var factory = settings.CreateConnectionFactory(automaticRecoveryEnabled: true);

            // Opening the persistent TCP socket
            var connection = await factory.CreateConnectionAsync(cancellationToken);

            //on shutdown event
            connection.ConnectionShutdownAsync += async (_, args) =>
            {
                if (_activeConsumers.TryGetValue(queueName, out var handle))
                {
                    handle.IsConnected = false;
                    handle.LastError = args.ReplyText;
                    handle.LastStatusChangeUtc = DateTime.UtcNow;
                }

                _logger.LogWarning(
                    "RabbitMQ connection shutdown for queue {QueueName}. Reason: {Reason}",
                    queueName,
                    args.ReplyText
                );

                await Task.CompletedTask;
            };

            //on recovery event
            connection.RecoverySucceededAsync += async (_, _) =>
            {
                if (_activeConsumers.TryGetValue(queueName, out var handle))
                {
                    handle.IsConnected = true;
                    handle.LastError = null;
                    handle.LastStatusChangeUtc = DateTime.UtcNow;
                }

                _logger.LogInformation(
                    "RabbitMQ connection recovered for queue {QueueName}",
                    queueName
                );

                await Task.CompletedTask;
            };

            // Creating a light-weight channle
            var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            //   Making sure queue exists before subscribing
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken
            );

            var consumer = new AsyncEventingBasicConsumer(channel);

            // will fire every damn time a single message hits the DLQ
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var rawPayload = Encoding.UTF8.GetString(body);

                    //  Tracking exception info from RabbitMQ message headers
                    string exceptionReason = "Unknown DLQ execution";
                    if (
                        ea.BasicProperties.Headers != null
                        && ea.BasicProperties.Headers.ContainsKey("x-first-death-reason")
                    )
                    {
                        var reasonBytes =
                            ea.BasicProperties.Headers["x-first-death-reason"] as byte[];
                        if (reasonBytes != null)
                            exceptionReason = Encoding.UTF8.GetString(reasonBytes);
                    }

                    var headers =
                        ea.BasicProperties.Headers?.ToDictionary(
                            header => header.Key,
                            header => HeaderValueToString(header.Value)
                        )
                        ?? new Dictionary<string, string>();

                    var dlqMessage = new DeadLetterMessage
                    {
                        MessageId = ea.BasicProperties.MessageId ?? Guid.NewGuid().ToString(),
                        QueueName = queueName,
                        RawPayload = rawPayload,
                        ExceptionMessage = exceptionReason,
                        Timestamp = DateTime.UtcNow,
                        CorrelationId = ea.BasicProperties.CorrelationId,
                        ContentType = ea.BasicProperties.ContentEncoding,
                        Type = ea.BasicProperties.ReplyTo,
                        Expiration = ea.BasicProperties.ReplyTo,
                        AppId = ea.BasicProperties.AppId,
                        Persistent = ea.BasicProperties.Persistent,
                        Priority = ea.BasicProperties.Priority,
                        Headers = headers,
                    };

                    //  Thread-safe update ✅✅
                    _indexStore.AddOrUpdate(dlqMessage);
                    _logger.LogInformation(
                        "Successfully indexed dead-lettered message {MessageId}",
                        dlqMessage.MessageId
                    );

                    //  Letting RabbitMQ know we've securely cataloged the message
                    await channel.BasicAckAsync(
                        ea.DeliveryTag,
                        multiple: false,
                        cancellationToken: CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed processing incoming dead-letter message payload");
                }
            };
            // Start listening on queue stream
            var consumerTag = await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken
            );

            _activeConsumers[queueName] = new QueueConsumerHandle(connection, channel, consumerTag);
        }
        finally
        {
            _consumerLock.Release();
        }
    }

    public async Task StopConsumingAsync(string queueName, CancellationToken cancellationToken)
    {
        if (!_activeConsumers.TryRemove(queueName, out var consumerHandle))
            return;

        _logger.LogInformation("Closing RabbitMQ consumer for queue {QueueName}...", queueName);
        await consumerHandle.Channel.BasicCancelAsync(
            consumerHandle.ConsumerTag,
            cancellationToken: cancellationToken
        );
        await consumerHandle.Channel.CloseAsync(cancellationToken: cancellationToken);
        await consumerHandle.Connection.CloseAsync(cancellationToken: cancellationToken);
    }

    private sealed class QueueConsumerHandle
    {
        public QueueConsumerHandle(IConnection connection, IChannel channel, string consumerTag)
        {
            Connection = connection;
            Channel = channel;
            ConsumerTag = consumerTag;
            IsConnected = true;
            LastError = null;
        }

        public IConnection Connection { get; }
        public IChannel Channel { get; }
        public string ConsumerTag { get; }
        public bool IsConnected { get; set; }
        public string? LastError { get; set; }
        public DateTime LastStatusChangeUtc { get; set; } = DateTime.UtcNow;
    }

    private static string HeaderValueToString(object? value)
    {
        if (value is null)
            return string.Empty;

        if (value is byte[] bytes)
            return Encoding.UTF8.GetString(bytes);

        return value.ToString() ?? string.Empty;
    }
}
