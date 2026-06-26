using ChronosDLQ.App.Services;

public class QueueConsumerWorker : BackgroundService
{
    private readonly IMessageBrokerConsumer _brokerConsumer;
    private readonly ILogger<QueueConsumerWorker> _logger;
    private const string TargetQueue = "orders.dlq";

    public QueueConsumerWorker(
        IMessageBrokerConsumer brokerConsumer,
        ILogger<QueueConsumerWorker> logger
    )
    {
        _brokerConsumer = brokerConsumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting background worker for queue: {QueueName}", TargetQueue);

        // keeping the worker running until host shuts down
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Starting the continuous consumption loop
                await _brokerConsumer.StartConsumingAsync(TargetQueue, stoppingToken);

                // keeping the bg worker alive while host runs
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Background worker cancellation requested for queue: {QueueName}",
                    TargetQueue
                );
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException)
            {
                _logger.LogWarning(
                    "RabbitMQ broker is unreachable at localhost:5672. Retrying connection in 5 seconds..."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occured inside the background worker execution loop"
                );
            }
            finally
            {
                _logger.LogInformation(
                    "Stopping communicaton channels for queue: {QueueName}",
                    TargetQueue
                );
                await _brokerConsumer.StopConsumingAsync(stoppingToken);
            }

            // reconnection fallback delay
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
