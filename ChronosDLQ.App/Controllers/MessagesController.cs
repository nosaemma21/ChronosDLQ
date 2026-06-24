using ChronosDLQ.App.Models;
using ChronosDLQ.App.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly IMessageIndexStore _indexStore;

    public MessagesController(IMessageIndexStore indexStore)
    {
        _indexStore = indexStore;
    }

    [HttpGet]
    public ActionResult<IEnumerable<DeadLetterMessage>> GetAllMessages()
    {
        var messages = _indexStore.GetAll();
        return Ok(messages);
    }

    [HttpGet("{messageId}")]
    public ActionResult<DeadLetterMessage> GetAMessage(string messageId)
    {
        var message = _indexStore.GetById(messageId);
        if (message == null)
        {
            return NotFound(
                new { message = $"Message with ID {messageId} not found in DLQ index store" }
            );
        }
        return Ok(message);
    }
}
