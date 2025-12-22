using ACARSPlugin.Server.Contracts;

namespace ACARSPlugin.Model;

public class AircraftConnection(string callsign, DataAuthorityState dataAuthorityState)
{
    public string Callsign { get; } = callsign;
    public DataAuthorityState DataAuthorityState { get; set; } = dataAuthorityState;
}