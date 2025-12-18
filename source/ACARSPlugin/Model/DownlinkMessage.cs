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
    public int? ReplyToUplinkId { get; } = null;
    public CpdlcDownlinkResponseType ResponseType { get; } = responseType;
    public string Content { get; } = content;
    public DateTimeOffset Received { get; } = received;

    public bool StoodBy { get; private set; }
    public bool Deferred { get; private set; }
    public bool UnableSent { get; private set; }
    public bool Completed { get; private set; }

    public void Standby()
    {
        StoodBy = true;
    }

    public void Defer()
    {
        Deferred = true;
    }

    public void Complete(bool unable)
    {
        StoodBy = false;
        Deferred = false;
        UnableSent = unable;
        Completed = true;
    }
}