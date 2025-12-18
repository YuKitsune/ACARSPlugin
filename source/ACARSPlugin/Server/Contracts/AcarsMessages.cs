namespace ACARSPlugin.Server.Contracts;

// Copied from https://github.com/YuKitsune/ACARSServer/blob/main/source/ACARSServer/Contracts/AcarsMessages.cs

public interface IUplinkMessage
{
    string Recipient { get; }
}

public interface IDownlinkMessage
{
    string Sender { get; }
}

public record TelexUplink(string Recipient, string Content) : IUplinkMessage;
public record TelexDownlink(string Sender, string Content) : IDownlinkMessage;

public enum CpdlcDownlinkResponseType
{
    NoResponse,
    ResponseRequired
}

public enum CpdlcUplinkResponseType
{
    NoResponse,
    WilcoUnable,
    AffirmativeNegative,
    Roger
}

public interface ICpdlcUplink : IUplinkMessage
{
    CpdlcUplinkResponseType ResponseType { get; } 
    string Content { get; }
}

public interface ICpdlcDownlink : IDownlinkMessage
{
    CpdlcDownlinkResponseType ResponseType { get; }  
    string Content { get; }
}

public interface ICpdlcReply
{
    public int ReplyToMessageId { get; }
}

public record CpdlcUplink(int MessageId, string Recipient, CpdlcUplinkResponseType ResponseType, string Content) : ICpdlcUplink;
public record CpdlcUplinkReply(int MessageId, string Recipient, int ReplyToMessageId, CpdlcUplinkResponseType ResponseType, string Content) : ICpdlcUplink, ICpdlcReply;

public record CpdlcDownlink(int MessageId, string Sender, CpdlcDownlinkResponseType ResponseType, string Content) : ICpdlcDownlink;
public record CpdlcDownlinkReply(int MessageId, string Sender, int ReplyToMessageId, CpdlcDownlinkResponseType ResponseType, string Content) : ICpdlcDownlink, ICpdlcReply;