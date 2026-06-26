using System.Collections.Concurrent;
using System.Text;
using ChronosDLQ.App.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChronosDLQ.App.Services;

class RabbitMqConsumer : IMessageBrokerConsumer
{
    private readonly IMessageIndexStore _indexStore;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly ConcurrentDictionary<string, QueueConsumerHandle> _activeConsumers = new();
    private readonly SemaphoreSlim _consumerLock = new(1, 1);

    public RabbitMqConsumer(
        IMessageIndexStore indexStore,
        IConfiguration configuration,
        ILogger<RabbitMqConsumer> logger
    )
    {
        _indexStore = indexStore;
        _configuration = configuration;
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

            _logger.LogInformation("Connecting to RabbitMQ broker for queue {QueueName}...", queueName);
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:HostName"] ?? "localhost",

            // checking for connection drops and auto-reconnect
            AutomaticRecoveryEnabled = true,

            //creating queues when reconnecting
            TopologyRecoveryEnabled = true,

            //Retry every 10 seconds
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
        };

        // Opening the persistent TCP socket
        var connection = await factory.CreateConnectionAsync(cancellationToken);

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
                    var reasonBytes = ea.BasicProperties.Headers["x-first-death-reason"] as byte[];
                    if (reasonBytes != null)
                        exceptionReason = Encoding.UTF8.GetString(reasonBytes);
                }

                var dlqMessage = new DeadLetterMessage
                {
                    MessageId = ea.BasicProperties.MessageId ?? Guid.NewGuid().ToString(),
                    QueueName = queueName,
                    RawPayload = rawPayload,
                    ExceptionMessage = exceptionReason,
                    Timestamp = DateTime.UtcNow,
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

            _activeConsumers[queueName] = new QueueConsumerHandle(
                connection,
                channel,
                consumerTag
            );
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

    private sealed record QueueConsumerHandle(
        IConnection Connection,
        IChannel Channel,
        string ConsumerTag
    );
}
