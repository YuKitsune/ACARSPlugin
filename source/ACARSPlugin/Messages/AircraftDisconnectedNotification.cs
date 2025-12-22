using ACARSPlugin.Services;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;

namespace ACARSPlugin.Messages;

public record AircraftDisconnectedNotification(string Callsign) : INotification;

public class AircraftDisconnectedNotificationHandler(AircraftConnectionTracker aircraftConnectionTracker)
    : INotificationHandler<AircraftDisconnectedNotification>
{
    public async Task Handle(AircraftDisconnectedNotification notification, CancellationToken cancellationToken)
    {
        await aircraftConnectionTracker.UnregisterConnection(notification.Callsign, cancellationToken);

        WeakReferenceMessenger.Default.Send(new ConnectedAircraftChanged());
    }
}