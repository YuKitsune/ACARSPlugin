using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;

namespace CPDLCServer.Handlers;

public class LogonCommandHandler(IAircraftRepository aircraftRepository, IClock clock, IMediator mediator)
    : IRequestHandler<LogonCommand>
{
    public async Task Handle(LogonCommand request, CancellationToken cancellationToken)
    {
        var aircraft = new AircraftConnection(
            request.Callsign,
            request.StationId,
            request.FlightSimulationNetwork,
            DataAuthorityState.NextDataAuthority);
        
        aircraft.RequestLogon(clock.UtcNow());
        
        // TODO: Perform validation
        // TODO: What if there are no controllers online?
        
        await aircraftRepository.Add(aircraft, cancellationToken);

        // Immediately accept it for now
        aircraft.AcceptLogon(clock.UtcNow());
        
        await mediator.Send(
            new SendUplinkCommand(
                "SYSTEM",
                request.FlightSimulationNetwork,
                request.StationId,
                request.Callsign,
                request.DownlinkId,
                CpdlcUplinkResponseType.NoResponse,
                "LOGON ACCEPTED"),
            cancellationToken);

        await mediator.Publish(
            new AircraftConnected(
                request.FlightSimulationNetwork,
                request.StationId,
                request.Callsign,
                aircraft.DataAuthorityState),
            cancellationToken);
    }
}