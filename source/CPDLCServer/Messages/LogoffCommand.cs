using MediatR;

namespace CPDLCServer.Messages;

public record LogoffCommand(
    int DownlinkId,
    string Callsign,
    string StationId,
    string FlightSimulationNetwork) : IRequest;