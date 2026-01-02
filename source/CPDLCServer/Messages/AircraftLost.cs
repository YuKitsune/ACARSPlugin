using MediatR;

namespace CPDLCServer.Messages;

public record AircraftLost(
    string AcarsClientId,
    string Callsign)
    : INotification;