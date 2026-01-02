using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record ConnectedNotification : INotification;

public class ConnectedNotificationHandler(Plugin plugin, DialogueStore dialogueStore, AircraftConnectionStore aircraftConnectionStore, ControllerConnectionStore controllerConnectionStore, ILogger logger)
    : INotificationHandler<ConnectedNotification>
{
    public async Task Handle(ConnectedNotification notification, CancellationToken cancellationToken)
    {
        logger.Information("Connected to server");
        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
        {
            logger.Warning("Not connected to server");
            return;
        }

        // Load dialogues
        logger.Debug("Loading all dialogues");
        var dialogues = await plugin.ConnectionManager.GetAllDialogues(cancellationToken);
        await dialogueStore.Populate(dialogues, cancellationToken);
        logger.Debug("Loaded {DialogueCount} dialogue(s)", dialogues.Length);

        // Load aircraft connections
        logger.Debug("Loading all aircraft connections");
        var connectedAircraft = await plugin.ConnectionManager.GetConnectedAircraft(cancellationToken);
        await aircraftConnectionStore.Populate(connectedAircraft, cancellationToken);
        logger.Debug("Loaded {ConnectionCount} aircraft connection(s)", connectedAircraft.Length);

        // Load controller connections
        logger.Debug("Loading all controller connections");
        var connectedControllers = await plugin.ConnectionManager.GetConnectedControllers(cancellationToken);
        await controllerConnectionStore.Populate(connectedControllers, cancellationToken);
        logger.Debug("Loaded {ControllerCount} controller connection(s)", connectedControllers.Length);

        // Relay notification to UI
        WeakReferenceMessenger.Default.Send(notification);
    }
}
