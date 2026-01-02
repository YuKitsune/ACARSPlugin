using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCServer.Messages;

public record GetConnectedControllersRequest(string FlightSimulationNetwork, string StationIdentifier)
    : IRequest<GetConnectedControllersResult>;

public record GetConnectedControllersResult(ControllerConnectionDto[] Controllers);
