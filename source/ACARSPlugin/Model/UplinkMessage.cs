using ACARSPlugin.Server.Contracts;

namespace ACARSPlugin.Model;

// TODO: Separate formatted and plaintext contents.

public class UplinkMessage(int id,
    string recipient,
    CpdlcUplinkResponseType responseType,
    string content,
    DateTimeOffset sent,
    bool isSpecial,
    int? replyToDownlinkId = null)
    : IAcarsMessageModel
{
    public int Id { get; } = id;
    public string Recipient { get; } = recipient;
    public int? ReplyToDownlinkId { get; } = replyToDownlinkId;
    int? IAcarsMessageModel.ReplyToMessageId => ReplyToDownlinkId;
    public CpdlcUplinkResponseType ResponseType { get; } = responseType;
    public string Content { get; } = content;
    public string FormattedContent => Content.Replace("@", string.Empty);
    public DateTimeOffset Sent { get; } = sent;
    public bool IsSpecial { get; } = isSpecial;

    public bool IsClosed { get; set; }
    public bool IsAcknowledged { get; set; }
    public bool IsManuallyAcknowledged { get; set; } // Pilot acknowledged via voice, no corresponding downlink
    public bool CanAction { get; set; }
    public bool Actioned { get; set; }
    public bool IsUrgent { get; set; }
    public bool IsPilotLate { get; set; }
    public bool IsTransmissionFailed { get; set; }

    DateTimeOffset IAcarsMessageModel.Time => Sent;
}