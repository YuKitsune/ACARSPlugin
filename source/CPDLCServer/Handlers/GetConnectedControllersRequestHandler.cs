using CPDLCServer.Contracts;
using CPDLCServer.Messages;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class GetConnectedControllersRequestHandler(IControllerRepository controllerRepository)
    : IRequestHandler<GetConnectedControllersRequest, GetConnectedControllersResult>
{
    public async Task<GetConnectedControllersResult> Handle(
        GetConnectedControllersRequest request,
        CancellationToken cancellationToken)
    {
        var controllers = await controllerRepository.All(
            request.FlightSimulationNetwork,
            request.StationIdentifier,
            cancellationToken);

        var controllerInfo = controllers
            .Select(c => new ControllerConnectionDto(
                c.Callsign,
                c.StationIdentifier,
                c.FlightSimulationNetwork,
                c.VatsimCid))
            .ToArray();

        return new GetConnectedControllersResult(controllerInfo);
    }
}
