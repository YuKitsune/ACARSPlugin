using MediatR;

namespace CPDLCServer.Messages;

public record LogonCommand(
    int DownlinkId,
    string Callsign,
    string AcarsClientId) : IRequest;