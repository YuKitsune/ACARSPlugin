using CommunityToolkit.Mvvm.Messaging;
using MediatR;

namespace ACARSPlugin.Messages;

public class ConnectedNotification(string StationId) : INotification;

public class ConnectedNotificationBridge : INotificationHandler<ConnectedNotification>
{
    public Task Handle(ConnectedNotification notification, CancellationToken cancellationToken)
    {
        WeakReferenceMessenger.Default.Send(notification);
        return Task.CompletedTask;
    }
}