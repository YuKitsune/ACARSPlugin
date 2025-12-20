namespace ACARSPlugin.Model;

/// <summary>
/// Represents a conversation thread between controller and pilot.
/// Groups all messages that are part of the same dialogue.
/// </summary>
public class Dialogue
{
    private readonly List<IAcarsMessageModel> _messages = [];

    public Dialogue(int rootMessageId, string callsign, IAcarsMessageModel firstMessage)
    {
        RootMessageId = rootMessageId;
        Callsign = callsign;
        Opened = GetMessageTime(firstMessage);
        _messages.Add(firstMessage);
        State = DialogueState.Open;
    }

    public int RootMessageId { get; }
    public string Callsign { get; }
    public IReadOnlyList<IAcarsMessageModel> Messages => _messages.AsReadOnly();
    public DialogueState State { get; set; }
    public DateTimeOffset Opened { get; }
    public DateTimeOffset? Closed { get; private set; }

    /// <summary>
    /// Adds a message to this dialogue.
    /// </summary>
    public void AddMessage(IAcarsMessageModel message)
    {
        _messages.Add(message);
    }

    /// <summary>
    /// Marks this dialogue as closed.
    /// </summary>
    public void Close()
    {
        State = DialogueState.ClosedPending;
        Closed = DateTimeOffset.UtcNow;

        // Update all message states to Closed
        foreach (var message in _messages)
        {
            switch (message)
            {
                case UplinkMessage uplink:
                    uplink.State = MessageState.Closed;
                    break;
                case DownlinkMessage downlink:
                    downlink.State = MessageState.Closed;
                    break;
            }
        }
    }

    /// <summary>
    /// Checks if the dialogue is complete (all messages are closed, no responses pending).
    /// </summary>
    public bool IsComplete()
    {
        // A dialogue is complete when all messages that require responses have been responded to
        foreach (var message in _messages)
        {
            switch (message)
            {
                case UplinkMessage uplink when uplink.ResponseType != Server.Contracts.CpdlcUplinkResponseType.NoResponse:
                    // Uplink requires response - check if we got one
                    var hasResponse = _messages.OfType<DownlinkMessage>()
                        .Any(dl => dl.ReplyToUplinkId == uplink.Id);
                    if (!hasResponse && uplink.State != MessageState.Closed)
                        return false;
                    break;

                case DownlinkMessage downlink when downlink.ResponseType == Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired:
                    // Downlink requires response - check if we sent one (excluding interim responses)
                    var hasSentResponse = _messages.OfType<UplinkMessage>()
                        .Any(ul => ul.ReplyToDownlinkId == downlink.Id &&
                                   !IsInterimResponse(ul.Content));
                    if (!hasSentResponse)
                        return false;
                    break;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if this dialogue should be transferred to history.
    /// </summary>
    /// <param name="now">The current time</param>
    /// <param name="historyTransferDelaySeconds">Number of seconds to wait after closing before transferring to history</param>
    public bool ShouldTransferToHistory(DateTimeOffset now, int historyTransferDelaySeconds)
    {
        if (State != DialogueState.ClosedPending || !Closed.HasValue)
            return false;

        // All messages must be acknowledged before transferring to history
        if (!AreAllMessagesAcknowledged())
            return false;

        var transferTime = Closed.Value.AddSeconds(historyTransferDelaySeconds);
        return now >= transferTime;
    }

    /// <summary>
    /// Checks if all messages in the dialogue are acknowledged.
    /// </summary>
    private bool AreAllMessagesAcknowledged()
    {
        foreach (var message in _messages)
        {
            switch (message)
            {
                case DownlinkMessage downlink when !downlink.IsAcknowledged:
                    return false;
                case UplinkMessage uplink when uplink.State != MessageState.Closed:
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if a STANDBY response has been sent for a downlink message.
    /// </summary>
    public bool HasStandbyResponse(int downlinkMessageId)
    {
        return _messages.OfType<UplinkMessage>()
            .Any(ul => ul.ReplyToDownlinkId == downlinkMessageId &&
                       ul.Content.Contains("STANDBY", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a DEFERRED response has been sent for a downlink message.
    /// </summary>
    public bool HasDeferredResponse(int downlinkMessageId)
    {
        return _messages.OfType<UplinkMessage>()
            .Any(ul => ul.ReplyToDownlinkId == downlinkMessageId &&
                       ul.Content.Contains("DEFERRED", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a message content is an interim response (STANDBY or DEFERRED).
    /// </summary>
    private static bool IsInterimResponse(string content)
    {
        return content.Contains("STANDBY", StringComparison.OrdinalIgnoreCase) ||
               content.Contains("DEFERRED", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTimeOffset GetMessageTime(IAcarsMessageModel message)
    {
        return message switch
        {
            DownlinkMessage downlink => downlink.Received,
            UplinkMessage uplink => uplink.Sent,
            _ => throw new ArgumentException($"Unknown message type: {message.GetType().Name}")
        };
    }
}
