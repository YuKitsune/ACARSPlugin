using ACARSPlugin.Server.Contracts;

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
    }

    public int RootMessageId { get; }
    public string Callsign { get; }
    public IReadOnlyList<IAcarsMessageModel> Messages => _messages.AsReadOnly();
    public bool IsInHistory { get; set; }
    public DateTimeOffset Opened { get; }
    public DateTimeOffset? Closed => CalculateClosedTime();
    public bool IsClosed => Closed.HasValue;

    /// <summary>
    /// Adds a message to this dialogue.
    /// </summary>
    public void AddMessage(IAcarsMessageModel message)
    {
        // TODO: Calculate if this will close the dialogue
        
        _messages.Add(message);
    }

    /// <summary>
    /// Checks if the dialogue is terminated (all messages closed and acknowledged).
    /// </summary>
    public bool IsTerminated()
    {
        // All non-special messages must be closed
        bool allNonSpecialMessagesClosed = _messages.All(m => m switch
        {
            UplinkMessage ul when !IsSpecialMessage(ul) => ul.IsClosed,
            UplinkMessage ul when IsSpecialMessage(ul) => true, // Special messages don't count
            DownlinkMessage dl => dl.IsClosed,
            _ => false
        });

        // All downlink messages must be acknowledged
        bool allDownlinksAcknowledged = _messages.OfType<DownlinkMessage>().All(dl => dl.IsAcknowledged);

        return allNonSpecialMessagesClosed && allDownlinksAcknowledged;
    }

    /// <summary>
    /// Checks if the dialogue is complete (no responses pending).
    /// </summary>
    public bool IsComplete()
    {
        // A dialogue is complete when all messages that require responses have been responded to
        foreach (var message in _messages)
        {
            switch (message)
            {
                case UplinkMessage uplink when uplink.ResponseType != CpdlcUplinkResponseType.NoResponse:
                    // Uplink requires response - check if we got one
                    var hasReceivedResponse = _messages.OfType<DownlinkMessage>()
                        .Any(dl => !IsSpecialMessage(dl) && dl.ReplyToUplinkId == uplink.Id && dl.ResponseType != CpdlcDownlinkResponseType.NoResponse);
                    if (!hasReceivedResponse)
                        return false;
                    return hasReceivedResponse;

                case DownlinkMessage downlink when downlink.ResponseType == CpdlcDownlinkResponseType.ResponseRequired:
                    // Downlink requires response - check if we sent one (excluding interim responses)
                    var hasSentResponse = _messages.OfType<UplinkMessage>()
                        .Any(ul => !IsSpecialMessage(ul) && ul.ReplyToDownlinkId == downlink.Id);
                    if (!hasSentResponse)
                        return false;
                    break;
            }
        }

        return true;
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
                case DownlinkMessage { IsAcknowledged: false }:
                case UplinkMessage { IsAcknowledged: false }:
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Calculates when the dialogue was closed based on the last closed message timestamp.
    /// </summary>
    private DateTimeOffset? CalculateClosedTime()
    {
        // Find the latest timestamp among all closed messages
        DateTimeOffset? latestClosedTime = null;

        foreach (var message in _messages)
        {
            bool isClosed = message switch
            {
                UplinkMessage ul => ul.IsClosed,
                DownlinkMessage dl => dl.IsClosed,
                _ => false
            };

            if (!isClosed)
                continue;

            var messageTime = message switch
            {
                UplinkMessage ul => ul.Sent,
                DownlinkMessage dl => dl.Received,
                _ => (DateTimeOffset?)null
            };

            if (messageTime.HasValue && (!latestClosedTime.HasValue || messageTime.Value > latestClosedTime.Value))
            {
                latestClosedTime = messageTime;
            }
        }

        return latestClosedTime;
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

    static bool IsSpecialMessage(DownlinkMessage downlinkMessage)
    {
        // TODO: Source from configuration
        // Some messages that don't require a response shouldn't contribute to marking a dialogue as complete
        
        return downlinkMessage.Content.Equals("STANDBY", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsSpecialMessage(UplinkMessage downlinkMessage)
    {
        // TODO: Source from configuration
        // Some messages that don't require a response shouldn't contribute to marking a dialogue as complete
        
        return downlinkMessage.Content.Equals("STANDBY", StringComparison.OrdinalIgnoreCase) ||
               downlinkMessage.Content.Contains("DEFERRED", StringComparison.OrdinalIgnoreCase);
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
