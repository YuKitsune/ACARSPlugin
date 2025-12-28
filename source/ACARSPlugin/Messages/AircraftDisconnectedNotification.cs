using ACARSPlugin.Services;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record AircraftDisconnectedNotification(string Callsign) : INotification;

public class AircraftDisconnectedNotificationHandler(AircraftConnectionTracker aircraftConnectionTracker, ILogger logger)
    : INotificationHandler<AircraftDisconnectedNotification>
{
    public async Task Handle(AircraftDisconnectedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Aircraft {Callsign} disconnected", notification.Callsign);

        await aircraftConnectionTracker.UnregisterConnection(notification.Callsign, cancellationToken);

        WeakReferenceMessenger.Default.Send(new ConnectedAircraftChanged());
    }
}