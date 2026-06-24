using ChronosDLQ.App.Models;
using ChronosDLQ.App.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly IMessageIndexStore _indexStore;
    private readonly IMessageReplayService _replayService;

    public MessagesController(IMessageIndexStore indexStore, IMessageReplayService replayService)
    {
        _indexStore = indexStore;
        _replayService = replayService;
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

    [HttpPatch("{messageId}")]
    public IActionResult PatchMessagePayload(
        string messageId,
        [FromBody] JsonPatchDocument<dynamic> patchDoc
    )
    {
        if (patchDoc == null)
            return BadRequest();

        // Fetching message from store
        var existingMessage = _indexStore.GetById(messageId);
        if (existingMessage == null)
            return NotFound();

        try
        {
            // Deserializing the raw payload string into a mutable dynamic object
            var deserializedObject = JsonConvert.DeserializeObject<dynamic>(
                existingMessage.RawPayload
            );

            // Reserialize object back to clean string fmt
            string updatedPayload = JsonConvert.SerializeObject(deserializedObject);
            existingMessage.RawPayload = updatedPayload;

            // commiting changes back to thread safe index
            _indexStore.AddOrUpdate(existingMessage);

            return Ok(existingMessage);
        }
        catch (Exception ex)
        {
            return BadRequest(
                new { message = "Failed to apply JSON patch mods", details = ex.Message }
            );
        }
    }

    [HttpPost("replay")]
    public async Task<IActionResult> ReplayMessage([FromBody] ReplayRequest request)
    {
        var replayed = await _replayService.ReplayMessageAsync(
            request.MessageId,
            request.TargetQueue,
            request.ModifiedPayload
        );

        if (!replayed)
            return NotFound(
                new { message = $"Message {request.MessageId} not found in DLQ index store" }
            );

        return Ok(new { message = "Message replayed successfully" });
    }
}
