using ACARSPlugin.Server.Contracts;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;

namespace ACARSPlugin.Messages;

public record DialogueChangedNotification(DialogueDto Dialogue) : INotification;

public class DialogueChangedNotificationHandler(DialogueStore dialogueStore)
    : INotificationHandler<DialogueChangedNotification>
{
    public async Task Handle(DialogueChangedNotification notification, CancellationToken cancellationToken)
    {
        await dialogueStore.Upsert(notification.Dialogue, cancellationToken);

        // Try to open the Current Messages Window

        WeakReferenceMessenger.Default.Send(notification);
    }
}
