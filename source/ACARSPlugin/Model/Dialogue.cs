using ACARSPlugin.Server.Contracts;

namespace ACARSPlugin.Model;

/// <summary>
/// Represents a conversation thread between controller and pilot.
/// Groups all messages that are part of the same dialogue.
/// </summary>
public class Dialogue
{
    readonly List<IAcarsMessageModel> _messages = [];
    DateTimeOffset? _closedTime;

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
        ProcessMessage(message);
    }

    void ProcessMessage(IAcarsMessageModel message)
    {
        switch (message)
        {
            case UplinkMessage uplink:
                // Uplink requiring no response are self-closing
                if (uplink.ResponseType == CpdlcUplinkResponseType.NoResponse)
                {
                    uplink.IsClosed = true;
                }

                // Close the corresponding downlink, unless this is a special message (i.e. STANDBY)
                if (!uplink.IsSpecial && uplink.ReplyToDownlinkId.HasValue)
                {
                    var downlink = _messages.OfType<DownlinkMessage>().FirstOrDefault(dl => dl.Id == uplink.ReplyToDownlinkId.Value);
                    if (downlink != null)
                    {
                        downlink.IsClosed = true;
                        downlink.IsAcknowledged = true; // Auto-acknowledge when replying
                    }
                }

                // Close the dialogue if this message doesn't require a response, and it's not a special message (i.e. STANDBY or REQUEST DEFERRED)
                if (!uplink.IsSpecial && uplink.ResponseType == CpdlcUplinkResponseType.NoResponse)
                {
                    Close(uplink.Sent);
                }

                break;

            case DownlinkMessage downlink:
                // Downlink requiring no response are self-closing
                if (downlink.ResponseType == CpdlcDownlinkResponseType.NoResponse)
                {
                    downlink.IsClosed = true;
                }

                // Close the corresponding uplink, unless this is a special message (i.e. STANDBY)
                if (!downlink.IsSpecial && downlink.ReplyToUplinkId.HasValue)
                {
                    var uplink = _messages.OfType<UplinkMessage>().FirstOrDefault(ul => ul.Id == downlink.ReplyToUplinkId.Value);
                    if (uplink != null)
                    {
                        uplink.IsClosed = true;
                        uplink.IsAcknowledged = true; // Auto-acknowledge when pilot responds
                    }
                }

                // Close the dialogue if this message doesn't require a response, and it's not a special message (i.e. STANDBY or REQUEST DEFERRED)
                if (!downlink.IsSpecial && downlink.ResponseType == CpdlcDownlinkResponseType.NoResponse)
                {
                    Close(downlink.Received);
                }

                break;
        }
    }

    public void Close(DateTimeOffset now)
    {
        _closedTime = now;

        // Ensure all messages in the dialogue are also closed
        foreach (var message in _messages)
        {
            message.IsClosed = true;
        }
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
