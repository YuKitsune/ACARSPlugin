using ACARSPlugin.Model;
using ACARSPlugin.Server.Contracts;
using ACARSPlugin.Services;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;

namespace ACARSPlugin.Messages;

public record AircraftConnectedNotification(string Callsign, DataAuthorityState DataAuthorityState) : INotification;

public class AircraftConnectedNotificationHandler(AircraftConnectionTracker aircraftConnectionTracker)
    : INotificationHandler<AircraftConnectedNotification>
{
    public async Task Handle(AircraftConnectedNotification notification, CancellationToken cancellationToken)
    {
        var connection = new AircraftConnection(notification.Callsign, notification.DataAuthorityState);
        await aircraftConnectionTracker.RegisterConnection(connection, cancellationToken);

        WeakReferenceMessenger.Default.Send(new ConnectedAircraftChanged());
    }
}
