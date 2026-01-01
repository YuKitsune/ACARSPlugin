using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public class DisconnectedNotification : INotification;

public class DisconnectedNotificationBridge(
    DialogueStore dialogueStore,
    AircraftConnectionStore aircraftConnectionStore,
    ILogger logger) : INotificationHandler<DisconnectedNotification>
{
    public async Task Handle(DisconnectedNotification notification, CancellationToken cancellationToken)
    {
        logger.Debug("Clearing dialogue store");
        await dialogueStore.Clear(cancellationToken);

        logger.Debug("Aircraft connection store");
        await aircraftConnectionStore.Clear(cancellationToken);
        WeakReferenceMessenger.Default.Send(notification);
    }
}
