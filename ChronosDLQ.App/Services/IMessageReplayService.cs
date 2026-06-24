namespace ChronosDLQ.App.Services;

public interface IMessageReplayService
{
    /// <summary>
    /// Publish a corrected payload back to its target system queue and clears it from the DLQ index
    /// </summary>
    /// <param name="messageId">ID of the dead queue message</param>
    /// <param name="targetQueue">Target queue to lay the modified payload</param>
    /// <param name="modifiedPayload">The new payload</param>
    /// <returns><see langword="true"/> if completed successfully and <see langword="false"/> for failed completion</returns>
    Task<bool> ReplayMessageAsync(string messageId, string targetQueue, string modifiedPayload);
}
