using System.Text.Json.Serialization;

namespace CPDLCServer.Clients;

public interface IAcarsNetworkConfiguration
{
    string FlightSimulationNetwork { get; }
    string StationIdentifier { get; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(HoppiesConfiguration), "Hoppie")]
public abstract class AcarsConfiguration : IAcarsNetworkConfiguration
{
    public required string FlightSimulationNetwork { get; init; }
    public required string StationIdentifier { get; init; }
}

public class HoppiesConfiguration : AcarsConfiguration
{
    public required Uri Url { get; init; }
    public required string AuthenticationCode { get; init; }
}