using ACARSPlugin.Model;
using ACARSPlugin.Server.Contracts;
using ACARSPlugin.Services;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record DownlinkMessageReceivedNotification(IDownlinkMessage DownlinkMessage) : INotification;

public class DownlinkMessageReceivedNotificationHandler(MessageRepository messageRepository, AircraftConnectionTracker aircraftConnectionTracker, IPublisher publisher, ILogger logger)
    : INotificationHandler<DownlinkMessageReceivedNotification>
{
    public async Task Handle(DownlinkMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.DownlinkMessage is not CpdlcDownlink cpdlcDownlink)
        {
            logger.Warning("Received non-CPDLC downlink message, ignoring");
            return;
        }

        logger.Information("Received downlink message {MessageId} from {Sender}",
            cpdlcDownlink.Id, notification.DownlinkMessage.Sender);

        await messageRepository.AddDownlinkMessage(cpdlcDownlink, cancellationToken);

        // Promote the aircraft to CDA
        // The server already does this, but I'm too lazy to implement the signalr stuff
        var connection = await aircraftConnectionTracker.GetConnectedAircraft(
            notification.DownlinkMessage.Sender,
            cancellationToken);
        if (connection is not null && connection.DataAuthorityState == DataAuthorityState.NextDataAuthority)
        {
            logger.Debug("Promoting {Callsign} from NextDataAuthority to CurrentDataAuthority", notification.DownlinkMessage.Sender);
            connection.DataAuthorityState = DataAuthorityState.CurrentDataAuthority;
        }

        await publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
    }
}