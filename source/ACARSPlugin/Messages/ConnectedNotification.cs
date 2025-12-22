using CommunityToolkit.Mvvm.Messaging;
using MediatR;

namespace ACARSPlugin.Messages;

public class ConnectedNotification(string StationId) : INotification;

public class ConnectedNotificationBridge : INotificationHandler<ConnectedNotification>
{
    public Task Handle(ConnectedNotification notification, CancellationToken cancellationToken)
    {
        // TODO: Load all historical messages.
        
        WeakReferenceMessenger.Default.Send(notification);
        return Task.CompletedTask;
    }
}