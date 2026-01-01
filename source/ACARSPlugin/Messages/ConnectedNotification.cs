using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record ConnectedNotification(string StationId) : INotification;

public class ConnectedNotificationHandler(Plugin plugin, DialogueStore dialogueStore, AircraftConnectionStore aircraftConnectionStore, ILogger logger)
    : INotificationHandler<ConnectedNotification>
{
    public async Task Handle(ConnectedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Connected to server as station {StationId}, loading all dialogues", notification.StationId);
        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
        {
            logger.Warning("Not connected to server");
            return;
        }

        // Load dialogues
        var dialogues = await plugin.ConnectionManager.GetAllDialogues(cancellationToken);
        await dialogueStore.Populate(dialogues, cancellationToken);
        logger.Information("Loaded {DialogueCount} dialogue(s) from server", dialogues.Length);

        // Load aircraft connections
        var connectedAircraft = await plugin.ConnectionManager.GetConnectedAircraft(cancellationToken);
        await aircraftConnectionStore.Populate(connectedAircraft, cancellationToken);
        logger.Information("Loaded {ConnectionCount} aircraft connection(s) from server", connectedAircraft.Length);

        // Relay notification to UI
        WeakReferenceMessenger.Default.Send(notification);
    }
}
