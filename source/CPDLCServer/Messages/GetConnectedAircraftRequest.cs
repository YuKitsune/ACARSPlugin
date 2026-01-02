using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCServer.Messages;

public record GetConnectedAircraftRequest(string FlightSimulationNetwork, string StationIdentifier)
    : IRequest<GetConnectedAircraftResult>;

public record GetConnectedAircraftResult(AircraftConnectionDto[] Aircraft);
