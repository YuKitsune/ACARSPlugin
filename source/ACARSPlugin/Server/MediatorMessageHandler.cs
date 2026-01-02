using ACARSPlugin.Messages;
using ACARSPlugin.Server.Contracts;
using MediatR;

namespace ACARSPlugin.Server;

public class MediatorMessageHandler(IMediator mediator) : IDownlinkHandlerDelegate
{
    public async Task DialogueChanged(DialogueDto dialogue, CancellationToken cancellationToken)
    {
        await mediator.Publish(new DialogueChangedNotification(dialogue), cancellationToken);
    }

    public async Task AircraftConnectionUpdated(AircraftConnectionDto aircraftConnectionDto, CancellationToken cancellationToken)
    {
        await mediator.Publish(new AircraftConnectionUpdatedNotification(aircraftConnectionDto), cancellationToken);
    }

    public async Task AircraftConnectionRemoved(string callsign, CancellationToken cancellationToken)
    {
        await mediator.Publish(new AircraftConnectionRemovedNotification(callsign), cancellationToken);
    }

    public async Task ControllerConnectionUpdated(ControllerConnectionDto controllerConnectionDto, CancellationToken cancellationToken)
    {
        await mediator.Publish(new ControllerConnectionUpdatedNotification(controllerConnectionDto), cancellationToken);
    }

    public async Task ControllerConnectionRemoved(string callsign, CancellationToken cancellationToken)
    {
        await mediator.Publish(new ControllerConnectionRemovedNotification(callsign), cancellationToken);
    }

    public void Error(Exception error)
    {
        Plugin.AddError(error, "Error from SignalR Handler Delegate");
        mediator.Send(new DisconnectRequest()).GetAwaiter().GetResult();
    }
}
