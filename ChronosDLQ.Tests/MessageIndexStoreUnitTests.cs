using ChronosDLQ.App.Models;
using ChronosDLQ.App.Services;

namespace ChronosDLQ.Tests;

public class MessageIndexStoreUnitTests
{
    private readonly MessageIndexStore _store;

    public MessageIndexStoreUnitTests()
    {
        // Fresh store
        _store = new MessageIndexStore();
    }

    [Fact]
    public void AddOrUpdate_ShouldOverwriteExistingMessage_WhenIdAlreadyExists()
    {
        // Arrange
        var original = new DeadLetterMessage
        {
            MessageId = "dum-dum-1",
            ExceptionMessage = "Original Error",
        };

        var updated = new DeadLetterMessage
        {
            MessageId = "dum-dum-1",
            ExceptionMessage = "Modified Error",
        };
        _store.AddOrUpdate(original);

        // Act
        _store.AddOrUpdate(updated);

        //   Assert
        var result = _store.GetById("dum-dum-1");
        Assert.Equal("Modified Error", result?.ExceptionMessage);
        Assert.Single(_store.GetAll());
    }

    [Fact]
    public void GetAll_ShouldReturnEmptyCollection_WhenNoMessagesIndexed()
    {
        // Act
        var result = _store.GetAll();

        //   Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetById_ShouldReturn_Null_WhenMessageIdNotFound()
    {
        // Act
        var result = _store.GetById("blah-blah-blah-doesn't-exist-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Remove_ShouldSilentlyProceed_WhenIdDoesNotExist()
    {
        // Act
        var exception = Record.Exception(() => _store.Remove("blah-blah-blah-doesn't-exist-id"));

        //   Assert
        Assert.Null(exception);
    }
}
