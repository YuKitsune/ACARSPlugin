using MediatR;

namespace CPDLCServer.Messages;

public record ControllerDisconnectedNotification(
    Guid UserId,
    string FlightSimulationNetwork,
    string StationIdentifier,
    string Callsign)
    : INotification;
