using System.Diagnostics.CodeAnalysis;

namespace CPDLCServer.Model;

// Messages that are related (e.g. a CPDLC downlink request, the corresponding CPDLC uplink clearance and the subsequent
// pilot response) constitute a CPDLC dialogue.
// - A CPDLC dialogue is open if any of the CPDLC messages in the dialogue are open;
// - A CPDLC dialogue is closed if all CPDLC messages in the dialogue are closed.
// - A CPDLC message is open if the aircraft or ground system has not yet received a required response.
// - A CPDLC message is closed if the aircraft or ground system either:
//   - does not require a response; or
//   - has already received a required response.
// - STANDBY and REQUEST DEFERRED responses do not close a downlink
// - STANDBY response does not close an uplink CPDLC message

public class Dialogue
{
    readonly List<ICpdlcMessage> _messages = [];

    public Dialogue(string aircraftCallsign, ICpdlcMessage firstMessage)
    {
        AircraftCallsign = aircraftCallsign;
        Opened = firstMessage.Time;
        AddMessage(firstMessage);
    }

    public Guid Id { get; } = Guid.NewGuid();

    public string AircraftCallsign { get; }
    public IReadOnlyList<ICpdlcMessage> Messages => _messages.AsReadOnly();
    public DateTimeOffset Opened { get; }
    public DateTimeOffset? Closed { get; private set; }

    [MemberNotNullWhen(true, nameof(Closed))]
    public bool IsClosed => Closed.HasValue;

    public DateTimeOffset? Archived { get; private set; }

    [MemberNotNullWhen(true, nameof(Archived))]
    public bool IsArchived => Archived.HasValue;

    public void AddMessage(ICpdlcMessage message)
    {
        _messages.Add(message);

        // Apply closure rules then check if dialogue closes
        ProcessMessage(message);
    }

    public void Archive(DateTimeOffset now)
    {
        Archived = now;
    }

    public void AcknowledgeDownlink(int downlinkMessageId, DateTimeOffset now)
    {
        var downlink = _messages.OfType<DownlinkMessage>()
            .FirstOrDefault(dl => dl.MessageId == downlinkMessageId);

        if (downlink is null)
            throw new InvalidOperationException($"Downlink message {downlinkMessageId} not found in dialogue");

        downlink.Acknowledge(now);
    }

    void ProcessMessage(ICpdlcMessage message)
    {
        switch (message)
        {
            // Close the corresponding downlink if able
            case UplinkMessage uplink when uplink.MessageReference is not null && CanCloseDownlink(uplink):
                {
                    var downlink = _messages.OfType<DownlinkMessage>().FirstOrDefault(dl => dl.MessageId == uplink.MessageReference.Value);
                    if (downlink != null)
                    {
                        downlink.Close(uplink.Sent);
                        downlink.Acknowledge(uplink.Sent); // Auto-acknowledge when replying
                    }
                }

                break;

            // Close the corresponding uplink if able
            case DownlinkMessage downlink when downlink.MessageReference is not null && CanCloseUplink(downlink):
                {
                    var uplink = _messages.OfType<UplinkMessage>().FirstOrDefault(ul => ul.MessageId == downlink.MessageReference.Value);
                    if (uplink != null)
                    {
                        uplink.Close(downlink.Received);
                    }
                }

                break;
        }

        TryClose(message.Time);
        TryArchive(message);
    }

    public void TryClose(DateTimeOffset now)
    {
        if (_messages.All(m => m.IsClosed))
        {
            Closed = now;
        }
    }

    void TryArchive(ICpdlcMessage message)
    {
        // Immediately archive when certain uplinks are added
        if (message is UplinkMessage uplink && ShouldAutoArchive(uplink))
        {
            Archive(uplink.Sent);
        }
    }

    bool CanCloseUplink(DownlinkMessage downlink)
    {
        // STANDBY responses do not close uplinks
        return !downlink.Content.Equals("STANDBY");
    }

    bool CanCloseDownlink(UplinkMessage uplinkMessage)
    {
        // STANDBY and REQUEST DEFERRED responses do not close downlinks
        return !uplinkMessage.Content.Equals("STANDBY") && !uplinkMessage.Content.Equals("REQUEST DEFERRED");
    }

    bool ShouldAutoArchive(UplinkMessage uplinkMessage)
    {
        // Auto-archive dialogues with these uplink messages
        return uplinkMessage.Content.Equals("LOGON ACCEPTED") ||
               uplinkMessage.Content.Equals("UNABLE DUE TO AIRSPACE RESTRICTIONS") ||
               uplinkMessage.Content.Equals("UNABLE DUE TO TRAFFIC");
    }
}
