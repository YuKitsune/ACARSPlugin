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

public interface ICpdlcMessage
{
    int Id { get; }
}

public interface ICpdlcUplink : IUplinkMessage, ICpdlcMessage
{
    CpdlcUplinkResponseType ResponseType { get; } 
    string Content { get; }
}

public interface ICpdlcDownlink : IDownlinkMessage, ICpdlcMessage
{
    CpdlcDownlinkResponseType ResponseType { get; }  
    string Content { get; }
}

public record CpdlcUplink(int Id, string Recipient, int? ReplyToDownlinkId, CpdlcUplinkResponseType ResponseType, string Content) : ICpdlcUplink;

public record CpdlcDownlink(int Id, string Sender, int? ReplyToUplinkId, CpdlcDownlinkResponseType ResponseType, string Content) : ICpdlcDownlink;

public record ConnectedAircraftInfo(
    string Callsign,
    string StationId,
    string FlightSimulationNetwork,
    DataAuthorityState DataAuthorityState);

public enum DataAuthorityState
{
    NextDataAuthority,
    CurrentDataAuthority
}