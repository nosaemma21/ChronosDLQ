using System.Collections.Concurrent;
using ChronosDLQ.App.Models;

namespace ChronosDLQ.App.Services;

public class MessageIndexStore : IMessageIndexStore
{
    // Initializing directory so it is never null
    // I could have as well used a singleton service
    // Using concurrent dictionary for thread safety

    /// <summary>
    /// The storage for the dead letter messages
    /// </summary>
    private readonly ConcurrentDictionary<string, DeadLetterMessage> _cache = new();

    public MessageIndexStore(ConcurrentDictionary<string, DeadLetterMessage> cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Will add a dead letter message to the cache
    /// </summary>
    /// <param name="message">The dead letter message to add</param>
    public void AddOrUpdate(DeadLetterMessage message)
    {
        _cache.AddOrUpdate(message.MessageId, message, (key, oldVal) => message);
    }

    /// <summary>
    /// Will get all dead letter messages in the cache
    /// </summary>
    /// <returns>All dead letter messages in the cache</returns>
    public IEnumerable<DeadLetterMessage> GetAll()
    {
        return _cache.Values;
    }

    /// <summary>
    /// Will get a particular dead letter message
    /// </summary>
    /// <param name="messageId">The Id of the dead letter message being queried</param>
    /// <returns>A single dead letter messsage</returns>
    public DeadLetterMessage? GetById(string messageId)
    {
        _cache.TryGetValue(messageId, out var message);
        return message;
    }

    /// <summary>
    /// Removes a dead letter message from the cache
    /// </summary>
    /// <param name="messageId">The Id of the message to be removed</param>
    public void Remove(string messageId)
    {
        _cache.TryRemove(messageId, out _);
    }
}
