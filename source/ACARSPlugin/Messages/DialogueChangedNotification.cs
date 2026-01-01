using ACARSPlugin.Server.Contracts;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record DialogueChangedNotification(DialogueDto Dialogue) : INotification;

public class DialogueChangedNotificationHandler(DialogueStore dialogueStore, ILogger logger)
    : INotificationHandler<DialogueChangedNotification>
{
    public async Task Handle(DialogueChangedNotification notification, CancellationToken cancellationToken)
    {
        logger.Debug("Upserting dialogue {DialogueId}", notification.Dialogue.Id);

        await dialogueStore.Upsert(notification.Dialogue, cancellationToken);

        WeakReferenceMessenger.Default.Send(notification);
    }
}
