namespace CPDLCServer.Messages;

public record UserContext(
    Guid Id,
    string ConnectionId,
    string FlightSimulationNetwork,
    string StationIdentifier,
    string Callsign);