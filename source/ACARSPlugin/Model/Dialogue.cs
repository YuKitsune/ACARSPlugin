using ACARSPlugin.Server.Contracts;

namespace ACARSPlugin.Model;

/// <summary>
/// Represents a conversation thread between controller and pilot.
/// Groups all messages that are part of the same dialogue.
/// </summary>
public class Dialogue
{
    private readonly List<IAcarsMessageModel> _messages = [];
    private DateTimeOffset? _closedTime;

    public Dialogue(int rootMessageId, string callsign, IAcarsMessageModel firstMessage)
    {
        RootMessageId = rootMessageId;
        Callsign = callsign;
        Opened = firstMessage.Time;
        AddMessage(firstMessage);
    }

    public int RootMessageId { get; }
    public string Callsign { get; }
    public IReadOnlyList<IAcarsMessageModel> Messages => _messages.AsReadOnly();
    public bool IsInHistory { get; set; }
    public DateTimeOffset Opened { get; }
    public DateTimeOffset? Closed => _closedTime;
    public bool IsClosed => Closed.HasValue;

    public void AddMessage(IAcarsMessageModel message)
    {
        _messages.Add(message);

        // Apply closure rules then check if dialogue closes
        ApplyMessageClosureRules(message);
        CheckIfDialogueCloses();
    }

    void ApplyMessageClosureRules(IAcarsMessageModel message)
    {
        switch (message)
        {
            case UplinkMessage uplink:
                // Rule 1: Uplink requiring no response is closed immediately
                if (uplink.ResponseType == CpdlcUplinkResponseType.NoResponse)
                {
                    uplink.IsClosed = true;
                }

                // Rule 4: When uplink reply is sent, close the downlink it's replying to
                if (uplink.ReplyToDownlinkId.HasValue)
                {
                    // Special uplinks (STANDBY, REQUEST DEFERRED) don't close the downlink
                    if (uplink.IsSpecial)
                        return;

                    var downlink = _messages.OfType<DownlinkMessage>().FirstOrDefault(dl => dl.Id == uplink.ReplyToDownlinkId.Value);
                    if (downlink != null)
                    {
                        downlink.IsClosed = true;
                        downlink.IsAcknowledged = true; // Auto-acknowledge when replying
                    }
                }
                break;

            case DownlinkMessage downlink:
                // Rule 3: Downlink requiring no response is closed immediately
                if (downlink.ResponseType == CpdlcDownlinkResponseType.NoResponse)
                {
                    downlink.IsClosed = true;
                }

                // Rule 2: When downlink reply is received, close the uplink it's replying to
                if (downlink.ReplyToUplinkId.HasValue)
                {
                    // Special downlinks (STANDBY) don't close the uplink
                    if (downlink.IsSpecial)
                        return;

                    var uplink = _messages.OfType<UplinkMessage>().FirstOrDefault(ul => ul.Id == downlink.ReplyToUplinkId.Value);
                    if (uplink != null)
                    {
                        uplink.IsClosed = true;
                        uplink.IsAcknowledged = true; // Auto-acknowledge when pilot responds
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Checks if all non-special messages are closed and sets the closed time if so.
    /// </summary>
    internal void CheckIfDialogueCloses()
    {
        // Don't recalculate if already closed
        if (_closedTime.HasValue)
            return;

        // Check if all non-special messages are closed
        var allNonSpecialMessagesClosed = _messages.All(m => m switch
        {
            UplinkMessage { IsSpecial: true } => true, // Special messages don't count
            UplinkMessage ul => ul.IsClosed,
            DownlinkMessage { IsSpecial: true } => true, // Special messages don't count
            DownlinkMessage dl => dl.IsClosed,
            _ => false
        });

        if (!allNonSpecialMessagesClosed)
            return;

        // All non-special messages are closed - find the latest closed message time
        DateTimeOffset? latestClosedTime = null;

        foreach (var message in _messages)
        {
            var isClosed = message switch
            {
                UplinkMessage { IsSpecial: true } => false, // Don't consider special messages
                UplinkMessage ul => ul.IsClosed,
                DownlinkMessage { IsSpecial: true } => false, // Don't consider special messages
                DownlinkMessage dl => dl.IsClosed,
                _ => false
            };

            if (!isClosed)
                continue;

            if (!latestClosedTime.HasValue || message.Time > latestClosedTime.Value)
            {
                latestClosedTime = message.Time;
            }
        }

        _closedTime = latestClosedTime;
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
}
