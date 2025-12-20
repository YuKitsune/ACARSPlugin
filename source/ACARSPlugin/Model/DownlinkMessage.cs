using ACARSPlugin.Server.Contracts;

namespace ACARSPlugin.Model;

public interface IAcarsMessageModel
{
    int Id { get; }
    string Content { get; }
}

public class DownlinkMessage(int id, string sender, CpdlcDownlinkResponseType responseType, string content, DateTimeOffset received, int? replyToUplinkId = null) : IAcarsMessageModel
{
    public int Id { get; } = id;
    public string Sender { get; } = sender;
    public int? ReplyToUplinkId { get; } = replyToUplinkId;
    public CpdlcDownlinkResponseType ResponseType { get; } = responseType;
    public string Content { get; } = content;
    public DateTimeOffset Received { get; } = received;

    public MessageState State { get; set; } = MessageState.Normal;
    public bool IsAcknowledged { get; set; }
}