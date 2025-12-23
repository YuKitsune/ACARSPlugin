using CommunityToolkit.Mvvm.Messaging;
using MediatR;

namespace ACARSPlugin.Messages;

public record HistoryMessagesChanged : INotification;

public class HistoryMessagesChangedBridge : INotificationHandler<HistoryMessagesChanged>
{
    public Task Handle(HistoryMessagesChanged notification, CancellationToken cancellationToken)
    {
        WeakReferenceMessenger.Default.Send(notification);
        return Task.CompletedTask;
    }
}
