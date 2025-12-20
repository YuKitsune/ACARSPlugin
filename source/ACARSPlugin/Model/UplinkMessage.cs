using ACARSPlugin.Server.Contracts;

namespace ACARSPlugin.Model;

// TODO: Separate formatted and plaintext contents.

public class UplinkMessage(int id, string recipient, CpdlcUplinkResponseType responseType, string content, DateTimeOffset sent, int? replyToDownlinkId = null) : IAcarsMessageModel
{
    public int Id { get; } = id;
    public string Recipient { get; } = recipient;
    public int? ReplyToDownlinkId { get; } = replyToDownlinkId;
    public CpdlcUplinkResponseType ResponseType { get; } = responseType;
    public string Content { get; } = content;
    public DateTimeOffset Sent { get; } = sent;

    // Boolean state properties
    public bool IsClosed { get; set; }
    public bool IsAcknowledged { get; set; }
    public bool IsUrgent { get; set; }
    public bool IsPilotLate { get; set; }
    public bool IsTransmissionFailed { get; set; }
    
    DateTimeOffset IAcarsMessageModel.Time => Sent;
}