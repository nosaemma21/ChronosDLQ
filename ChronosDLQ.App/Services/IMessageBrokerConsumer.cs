namespace ChronosDLQ.App.Services;

public interface IMessageBrokerConsumer
{
    /// <summary>
    /// Opens a connection to the broker and listens to the designated queue
    /// </summary>
    /// <param name="queueName">Name of the designated queue</param>
    /// <param name="cancellationToken">Token for cancelling the operation</param>
    /// <returns>A task</returns>
    Task StartConsumingAsync(string queueName, CancellationToken cancellationToken);

    /// <summary>
    /// Gracefully closes the active channel for a queue
    /// </summary>
    /// <param name="queueName">Name of the queue to stop consuming</param>
    /// <param name="cancellationToken">Token for cancelling operation</param>
    /// <returns>A task</returns>
    Task StopConsumingAsync(string queueName, CancellationToken cancellationToken);
}
