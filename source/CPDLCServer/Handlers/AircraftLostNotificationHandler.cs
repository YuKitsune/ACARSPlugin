using CPDLCServer.Hubs;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Services;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CPDLCServer.Handlers;

public class AircraftLostNotificationHandler(
    IAircraftRepository aircraftRepository,
    IControllerRepository controllerRepository,
    IDialogueRepository dialogueRepository,
    IHubContext<ControllerHub> hubContext,
    IMessageIdProvider messageIdProvider,
    IPublisher publisher,
    IClock clock,
    ILogger logger)
    : INotificationHandler<AircraftLost>
{
    public async Task Handle(AircraftLost notification, CancellationToken cancellationToken)
    {
        var aircraft = await aircraftRepository.Find(
            notification.FlightSimulationNetwork,
            notification.StationId,
            notification.Callsign,
            cancellationToken);

        if (aircraft is null)
        {
            logger.Information(
                "Aircraft {Callsign} already removed from tracking on {Network}/{StationId}",
                notification.Callsign,
                notification.FlightSimulationNetwork,
                notification.StationId);
            return;
        }

        // Remove aircraft from tracking
        await aircraftRepository.Remove(
            notification.FlightSimulationNetwork,
            notification.StationId,
            notification.Callsign,
            cancellationToken);

        logger.Information(
            "Aircraft {Callsign} lost on {Network}/{StationId}",
            notification.Callsign,
            notification.FlightSimulationNetwork,
            notification.StationId);

        // Find all controllers on the same network and station
        var controllers = await controllerRepository.All(
            notification.FlightSimulationNetwork,
            notification.StationId, cancellationToken);

        if (!controllers.Any())
        {
            logger.Information(
                "No controllers to notify about lost aircraft {Callsign}",
                notification.Callsign);
            return;
        }

        // Find all dialogues for this aircraft on this station
        var allDialogues = await dialogueRepository.AllForStation(
            notification.FlightSimulationNetwork,
            notification.StationId,
            cancellationToken);

        var aircraftDialogues = allDialogues
            .Where(d => d.AircraftCallsign == notification.Callsign && !d.IsArchived)
            .ToArray();

        var openDialogues = aircraftDialogues.Where(d => !d.IsClosed).ToArray();

        if (openDialogues.Any())
        {
            // Add error message to each open dialogue
            foreach (var dialogue in openDialogues)
            {
                // Find an open message to reference
                var openMessage = dialogue.Messages.FirstOrDefault(m => !m.IsClosed);

                if (openMessage is null)
                {
                    logger.Warning(
                        "Dialogue {DialogueId} is marked as open but has no open messages",
                        dialogue.Id);
                    continue;
                }

                var messageId = await messageIdProvider.GetNextMessageId(
                    notification.StationId,
                    notification.Callsign,
                    cancellationToken);

                var errorDownlink = new DownlinkMessage(
                    messageId,
                    openMessage.MessageId,
                    notification.Callsign,
                    CpdlcDownlinkResponseType.NoResponse,
                    AlertType.Medium,
                    "ERROR CONNECTION TIMED OUT",
                    clock.UtcNow());

                dialogue.AddMessage(errorDownlink);

                // Publish DialogueChangedNotification
                await publisher.Publish(new DialogueChangedNotification(dialogue), cancellationToken);

                logger.Information(
                    "Added error message to dialogue {DialogueId} for lost aircraft {Callsign}",
                    dialogue.Id,
                    notification.Callsign);
            }
        }
        else
        {
            // No open dialogues, create a new one with the error message
            var messageId = await messageIdProvider.GetNextMessageId(
                notification.StationId,
                notification.Callsign,
                cancellationToken);

            var errorDownlink = new DownlinkMessage(
                messageId,
                null,
                notification.Callsign,
                CpdlcDownlinkResponseType.NoResponse,
                AlertType.Medium,
                "ERROR CONNECTION TIMED OUT",
                clock.UtcNow());

            var dialogue = new Dialogue(
                notification.FlightSimulationNetwork,
                notification.StationId,
                notification.Callsign,
                errorDownlink);

            await dialogueRepository.Add(dialogue, cancellationToken);

            // Publish DialogueChangedNotification
            await publisher.Publish(new DialogueChangedNotification(dialogue), cancellationToken);

            logger.Information(
                "Created new dialogue {DialogueId} with error message for lost aircraft {Callsign}",
                dialogue.Id,
                notification.Callsign);
        }

        // Notify controllers that the aircraft has disconnected
        var controllerConnectionIds = controllers.Select(c => c.ConnectionId).ToArray();
        await hubContext.Clients
            .Clients(controllerConnectionIds)
            .SendAsync("AircraftConnectionRemoved", notification.Callsign, cancellationToken);

        logger.Information(
            "Notified {ControllerCount} controller(s) about lost aircraft {Callsign}",
            controllers.Length,
            notification.Callsign);
    }
}
