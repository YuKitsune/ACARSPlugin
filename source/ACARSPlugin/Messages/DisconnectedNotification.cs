using CommunityToolkit.Mvvm.Messaging;
using MediatR;

namespace ACARSPlugin.Messages;

public class DisconnectedNotification : INotification;

public class DisconnectedNotificationBridge : INotificationHandler<DisconnectedNotification>
{
    public Task Handle(DisconnectedNotification notification, CancellationToken cancellationToken)
    {
        WeakReferenceMessenger.Default.Send(notification);
        return Task.CompletedTask;
    }
}