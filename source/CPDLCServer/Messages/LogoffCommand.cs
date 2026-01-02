using MediatR;

namespace CPDLCServer.Messages;

public record LogoffCommand(
    int DownlinkId,
    string Callsign,
    string AcarsClientId) : IRequest;