using ChronosDLQ.App.Models;
using ChronosDLQ.App.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class QueuesController : ControllerBase
{
    private readonly IQueueDiscoveryService _queueDiscoveryService;
    private readonly IQueueWatchService _queueWatchService;

    public QueuesController(
        IQueueDiscoveryService queueDiscoveryService,
        IQueueWatchService queueWatchService
    )
    {
        _queueDiscoveryService = queueDiscoveryService;
        _queueWatchService = queueWatchService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RabbitMqQueueInfo>>> GetQueues(
        CancellationToken cancellationToken
    )
    {
        try
        {
            var queues = await _queueDiscoveryService.GetQueuesAsync(cancellationToken);
            return Ok(queues);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("watched")]
    public ActionResult<IEnumerable<string>> GetWatchedQueues()
    {
        return Ok(_queueWatchService.GetWatchedQueues());
    }

    [HttpPost("watched")]
    public async Task<IActionResult> WatchQueue(
        [FromBody] WatchQueueRequest request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.QueueName))
            return BadRequest(new { message = "Queue name is required" });

        try
        {
            await _queueWatchService.WatchQueueAsync(request.QueueName, cancellationToken);
            return Ok(new { message = $"Chronos is now watching {request.QueueName}" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("watched/{*queueName}")]
    public async Task<IActionResult> UnwatchQueue(
        string queueName,
        CancellationToken cancellationToken
    )
    {
        await _queueWatchService.UnwatchQueueAsync(queueName, cancellationToken);
        return Ok(new { message = $"Chronos stopped watching {queueName}" });
    }
}
