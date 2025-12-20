using CommunityToolkit.Mvvm.Messaging;
using MediatR;

namespace ACARSPlugin.Messages;

public record CurrentMessagesChanged : INotification;

public class CurrentMessagesChangedBridge : INotificationHandler<CurrentMessagesChanged>
{
    public Task Handle(CurrentMessagesChanged notification, CancellationToken cancellationToken)
    {
        WeakReferenceMessenger.Default.Send(notification);
        return Task.CompletedTask;
    }
}