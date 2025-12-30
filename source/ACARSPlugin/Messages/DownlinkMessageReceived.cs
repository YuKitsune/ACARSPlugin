using ACARSPlugin.Model;
using ACARSPlugin.Server.Contracts;
using ACARSPlugin.Services;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record DownlinkMessageReceivedNotification(IDownlinkMessage DownlinkMessage) : INotification;

public class DownlinkMessageReceivedNotificationHandler(
    MessageRepository messageRepository,
    AircraftConnectionTracker aircraftConnectionTracker,
    IMediator mediator,
    ILogger logger)
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

        var trackingController = await mediator.Send(
            new GetTrackingControllerRequest(notification.DownlinkMessage.Sender),
            cancellationToken);

        await messageRepository.AddDownlinkMessage(cpdlcDownlink, trackingController.ControllerCallsign, cancellationToken);

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

        await mediator.Publish(new CurrentMessagesChanged(), cancellationToken);
    }
}
