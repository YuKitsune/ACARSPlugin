using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record AircraftConnectionRemovedNotification(string Callsign) : INotification;

public class AircraftConnectionRemovedNotificationNotificationHandler(AircraftConnectionStore aircraftConnectionStore, ILogger logger)
    : INotificationHandler<AircraftConnectionRemovedNotification>
{
    public async Task Handle(AircraftConnectionRemovedNotification notification, CancellationToken cancellationToken)
    {
        logger.Debug("Aircraft {Callsign} disconnected", notification.Callsign);

        if (!await aircraftConnectionStore.Remove(notification.Callsign, cancellationToken))
        {
            logger.Warning("Aircraft {Callsign} was not tracked", notification.Callsign);
        }

        WeakReferenceMessenger.Default.Send(new ConnectedAircraftChanged());
    }
}
