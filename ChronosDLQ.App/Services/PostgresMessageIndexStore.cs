using ChronosDLQ.App.Data;
using ChronosDLQ.App.Models;
using Microsoft.EntityFrameworkCore;

namespace ChronosDLQ.App.Services;

public class PostgresMessageIndexStore : IMessageIndexStore
{
    private readonly IDbContextFactory<ChronosDbContext> _dbContextFactory;

    public PostgresMessageIndexStore(IDbContextFactory<ChronosDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public void AddOrUpdate(DeadLetterMessage message)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();

        var existingMessage = dbContext.DeadLetterMessages.Find(message.MessageId);

        if (existingMessage == null)
        {
            dbContext.DeadLetterMessages.Add(message);
        }
        else
        {
            dbContext.Entry(existingMessage).CurrentValues.SetValues(message);
            existingMessage.Metadata = message.Metadata;
            existingMessage.Headers = message.Headers;
        }

        dbContext.SaveChanges();
    }

    public IEnumerable<DeadLetterMessage> GetAll()
    {
        using var dbContext = _dbContextFactory.CreateDbContext();

        return dbContext
            .DeadLetterMessages.AsNoTracking()
            .OrderByDescending(message => message.Timestamp)
            .ToList();
    }

    public DeadLetterMessage? GetById(string messageId)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();

        return dbContext
            .DeadLetterMessages.AsNoTracking()
            .FirstOrDefault(message => message.MessageId == messageId);
    }

    public void Remove(string messageId)
    {
        TryRemoveMessage(messageId);
    }

    public bool TryRemoveMessage(string messsageId)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();

        var existingMessage = dbContext.DeadLetterMessages.Find(messsageId);
        if (existingMessage == null)
            return false;

        dbContext.DeadLetterMessages.Remove(existingMessage);
        dbContext.SaveChanges();

        return true;
    }
}
