using ChronosDLQ.App.Models;

namespace ChronosDLQ.App.Services;

/// <summary>
/// Contract for decoupled controllers
/// </summary>
public interface IMessageIndexStore
{
    void AddOrUpdate(DeadLetterMessage message);
    IEnumerable<DeadLetterMessage> GetAll();
    DeadLetterMessage? GetById(string messageId);
    void Remove(string messageId);
    bool TryRemoveMessage(string messsageId);
}
