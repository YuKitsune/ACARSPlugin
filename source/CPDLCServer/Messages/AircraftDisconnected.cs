using MediatR;

namespace CPDLCServer.Messages;

public record AircraftDisconnected(
    string FlightSimulationNetwork,
    string StationId,
    string Callsign)
    : INotification;