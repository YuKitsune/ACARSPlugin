using CommunityToolkit.Mvvm.Messaging;
using MediatR;

namespace ACARSPlugin.Messages;

public class DisconnectedNotification : INotification;

public class DisconnectedNotificationBridge(DialogueStore dialogueStore, AircraftConnectionStore aircraftConnectionStore) : INotificationHandler<DisconnectedNotification>
{
    public async Task Handle(DisconnectedNotification notification, CancellationToken cancellationToken)
    {
        await dialogueStore.Clear(cancellationToken);
        await aircraftConnectionStore.Clear(cancellationToken);
        WeakReferenceMessenger.Default.Send(notification);
    }
}
