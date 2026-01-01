using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record AircraftConnectionRemovedNotification(string Callsign) : INotification;

public class AircraftConnectionRemovedNotificationNotificationHandler(AircraftConnectionStore aircraftConnectionStore, ILogger logger)
    : INotificationHandler<AircraftConnectionRemovedNotification>
{
    public async Task Handle(AircraftConnectionRemovedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Aircraft {Callsign} disconnected", notification.Callsign);

        await aircraftConnectionStore.Remove(notification.Callsign, cancellationToken);

        WeakReferenceMessenger.Default.Send(new ConnectedAircraftChanged());
    }
}
