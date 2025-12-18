using ACARSPlugin.Server.Contracts;

namespace ACARSPlugin.Model;

public class UplinkMessage(int id, string recipient, CpdlcUplinkResponseType responseType, string content, DateTimeOffset sent, int? replyToDownlinkId = null) : IAcarsMessageModel
{
    public int Id { get; } = id;
    public string Recipient { get; } = recipient;
    public int? ReplyToDownlinkId { get; } = replyToDownlinkId;
    public CpdlcUplinkResponseType ResponseType { get; } = responseType;
    public string Content { get; } = content;
    public DateTimeOffset Sent { get; } = sent;
}