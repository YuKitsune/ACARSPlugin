using MediatR;

namespace CPDLCServer.Messages;

public record ControllerConnectedNotification(
    Guid UserId,
    string FlightSimulationNetwork,
    string Callsign,
    string StationIdentifier)
    : INotification;