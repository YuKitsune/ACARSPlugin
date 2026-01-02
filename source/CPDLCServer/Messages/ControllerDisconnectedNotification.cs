using MediatR;

namespace CPDLCServer.Messages;

public record ControllerDisconnectedNotification(Guid UserId, string Callsign) : INotification;
