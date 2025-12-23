using ACARSPlugin.Server.Contracts;

namespace ACARSPlugin.Model;

public class DownlinkMessage(
    int id,
    string sender,
    CpdlcDownlinkResponseType responseType,
    string content,
    DateTimeOffset received,
    bool isSpecial,
    int? replyToUplinkId = null)
    : IAcarsMessageModel
{
    public int Id { get; } = id;
    public string Sender { get; } = sender;
    public int? ReplyToUplinkId { get; } = replyToUplinkId;
    int? IAcarsMessageModel.ReplyToMessageId => ReplyToUplinkId;
    public CpdlcDownlinkResponseType ResponseType { get; } = responseType;
    public string Content { get; } = content;
    public DateTimeOffset Received { get; } = received;
    public bool IsSpecial { get; } = isSpecial;

    public bool IsClosed { get; set; }
    public bool IsAcknowledged { get; set; }
    public bool IsUrgent { get; set; }
    public bool IsControllerLate { get; set; }

    DateTimeOffset IAcarsMessageModel.Time => Received;
}