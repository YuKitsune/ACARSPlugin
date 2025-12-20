using ACARSPlugin.Model;
using ACARSPlugin.Server.Contracts;
using MediatR;

namespace ACARSPlugin.Messages;

public record DownlinkMessageReceivedNotification(IDownlinkMessage DownlinkMessage) : INotification;

public class DownlinkMessageReceivedNotificationHandler(MessageRepository messageRepository, IPublisher publisher)
    : INotificationHandler<DownlinkMessageReceivedNotification>
{
    public async Task Handle(DownlinkMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.DownlinkMessage is not ICpdlcDownlink cpdlcDownlink)
            return;

        await messageRepository.AddDownlinkMessage(cpdlcDownlink, cancellationToken);
        await publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
    }
}