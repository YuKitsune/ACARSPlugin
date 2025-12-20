namespace ACARSPlugin.Model;

/// <summary>
/// Groups all dialogues for a specific aircraft.
/// Provides a unified view of all message exchanges with that aircraft.
/// </summary>
public class DialogueGroup
{
    private readonly List<Dialogue> _dialogues = [];

    public DialogueGroup(string callsign)
    {
        Callsign = callsign;
    }

    public string Callsign { get; }
    public IReadOnlyList<Dialogue> Dialogues => _dialogues.AsReadOnly();

    /// <summary>
    /// The time of the first message in the earliest dialogue.
    /// Used for sorting dialogue groups.
    /// </summary>
    public DateTimeOffset FirstMessageTime
    {
        get
        {
            if (_dialogues.Count == 0)
                return DateTimeOffset.MaxValue;

            return _dialogues.Min(d => d.Opened);
        }
    }

    /// <summary>
    /// Adds a dialogue to this group.
    /// </summary>
    public void AddDialogue(Dialogue dialogue)
    {
        if (dialogue.Callsign != Callsign)
            throw new ArgumentException($"Dialogue callsign '{dialogue.Callsign}' does not match group callsign '{Callsign}'");

        _dialogues.Add(dialogue);
    }

    /// <summary>
    /// Gets all messages from all dialogues, sorted by time.
    /// This provides the flattened view required for the Current Messages Window.
    /// </summary>
    public IEnumerable<IAcarsMessageModel> GetAllMessagesSortedByTime()
    {
        return _dialogues
            .SelectMany(d => d.Messages)
            .OrderBy(GetMessageTime);
    }

    /// <summary>
    /// Checks if this dialogue group has any current (non-history) messages.
    /// </summary>
    public bool HasCurrentMessages()
    {
        return _dialogues.Any(d => d.State != DialogueState.InHistory);
    }

    /// <summary>
    /// Removes a dialogue from this group.
    /// </summary>
    public void RemoveDialogue(Dialogue dialogue)
    {
        _dialogues.Remove(dialogue);
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
