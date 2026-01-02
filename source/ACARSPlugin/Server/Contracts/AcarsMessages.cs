using System.Text.Json.Serialization;

namespace ACARSPlugin.Server.Contracts;

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

public enum AlertType
{
    High,
    Medium,
    Low,
    None
}

public record AircraftConnectionDto(
    string Callsign,
    string StationId,
    string FlightSimulationNetwork,
    DataAuthorityState DataAuthorityState);

public record ControllerConnectionDto(
    string Callsign,
    string StationId,
    string FlightSimulationNetwork,
    string VatsimCid);

public record DialogueDto(
    Guid Id,
    string AircraftCallsign,
    IReadOnlyList<CpdlcMessageDto> Messages,
    DateTimeOffset Opened,
    DateTimeOffset? Closed,
    DateTimeOffset? Archived)
{
    public bool IsClosed => Closed is not null;
    public bool IsArchived => Archived is not null;
}

[JsonDerivedType(typeof(UplinkMessageDto), "uplink")]
[JsonDerivedType(typeof(DownlinkMessageDto), "downlink")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
public abstract class CpdlcMessageDto
{
    public required int MessageId { get; init; }

    public int? MessageReference { get; init; }

    public required AlertType AlertType { get; init; }

    public abstract DateTimeOffset Time { get; }

    public DateTimeOffset? Closed { get; init; }

    public DateTimeOffset? Acknowledged { get; init; }

    [JsonIgnore]
    public bool IsClosed => Closed is not null;

    [JsonIgnore]
    public bool IsAcknowledged => Acknowledged is not null;
}

public class UplinkMessageDto : CpdlcMessageDto
{
    public override DateTimeOffset Time => Sent;

    public required string Recipient { get; init; }

    public required string SenderCallsign { get; init; }

    public required CpdlcUplinkResponseType ResponseType { get; init; }

    public required string Content { get; init; }

    public required DateTimeOffset Sent { get; init; }

    public required bool IsPilotLate { get; init; }

    public required bool IsTransmissionFailed { get; init; }

    public required bool IsClosedManually { get; init; }

    public bool IsSpecial => Content is "STANDBY" or "REQUEST DEFERRED";
}

public class DownlinkMessageDto : CpdlcMessageDto
{
    public override DateTimeOffset Time => Received;

    public required string Sender { get; init; }

    public required CpdlcDownlinkResponseType ResponseType { get; init; }

    public required string Content { get; init; }

    public required DateTimeOffset Received { get; init; }

    public required bool IsControllerLate { get; init; }
}

public enum DataAuthorityState
{
    NextDataAuthority,
    CurrentDataAuthority
}
