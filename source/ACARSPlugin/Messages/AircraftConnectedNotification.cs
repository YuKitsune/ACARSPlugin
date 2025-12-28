using ACARSPlugin.Model;
using ACARSPlugin.Server.Contracts;
using ACARSPlugin.Services;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record AircraftConnectedNotification(string Callsign, DataAuthorityState DataAuthorityState) : INotification;

public class AircraftConnectedNotificationHandler(AircraftConnectionTracker aircraftConnectionTracker, ILogger logger)
    : INotificationHandler<AircraftConnectedNotification>
{
    public async Task Handle(AircraftConnectedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Aircraft {Callsign} connected with data authority state: {DataAuthorityState}",
            notification.Callsign, notification.DataAuthorityState);

        var connection = new AircraftConnection(notification.Callsign, notification.DataAuthorityState);
        await aircraftConnectionTracker.RegisterConnection(connection, cancellationToken);

        WeakReferenceMessenger.Default.Send(new ConnectedAircraftChanged());
    }
}
