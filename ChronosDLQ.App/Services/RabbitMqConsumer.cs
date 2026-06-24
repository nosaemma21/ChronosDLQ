using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using ChronosDLQ.App.Models;
using ChronosDLQ.App.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;

class RabbitMqConsumer : IMessageBrokerConsumer
{
    private readonly IMessageIndexStore _indexStore;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqConsumer(IMessageIndexStore indexStore, ILogger<RabbitMqConsumer> logger)
    {
        _indexStore = indexStore;
        _logger = logger;
    }

    public async Task StartConsumingAsync(string queueName, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Connecting to RabbitMQ broker...");
        var factory = new ConnectionFactory { HostName = "localhost" };

        // Opening the persistent TCP socket
        _connection = await factory.CreateConnectionAsync(cancellationToken);

        // Creating a light-weight channle
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        //   Making sure queue exists before subscribing
        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);

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
                    dlqMessage
                );

                //  Letting RabbitMQ know we've securely cataloged the message
                await _channel.BasicAckAsync(
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
        await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken
        );
    }

    public async Task StopConsumingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Closing active RabbitMQ connection channels...");
        if (_channel != null)
            await _channel.CloseAsync(cancellationToken: cancellationToken);
        if (_connection != null)
            await _connection.CloseAsync(cancellationToken: cancellationToken);
    }
}
