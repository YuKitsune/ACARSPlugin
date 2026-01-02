using MediatR;

namespace CPDLCServer.Messages;

public record AircraftLost(
    string FlightSimulationNetwork,
    string StationId,
    string Callsign)
    : INotification;