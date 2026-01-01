using ACARSPlugin.Server;
using MediatR;
using Serilog;
using vatsys;

namespace ACARSPlugin.Messages;

public record ConnectRequest(string ServerEndpoint, string StationId) : IRequest;

public class ConnectRequestHandler(Plugin plugin, IMediator mediator, ILogger logger) : IRequestHandler<ConnectRequest>
{
    public async Task Handle(ConnectRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Processing connect request to {ServerEndpoint} for station {StationId}",
            request.ServerEndpoint, request.StationId);

        // If already connected, disconnect first
        if (plugin.ConnectionManager is not null)
        {
            logger.Information("Existing connection found, disconnecting first");
            if (plugin.ConnectionManager.IsConnected)
            {
                await plugin.ConnectionManager.StopAsync();
            }

            plugin.ConnectionManager.Dispose();
            plugin.ConnectionManager = null;
        }

        if (!Network.IsConnected)
        {
            logger.Error("Cannot connect to ACARS server: not connected to VATSIM");
            throw new Exception("Not connected to VATSIM");
        }

        logger.Debug("Creating SignalR connection manager");
        var downlinkHandler = new MediatorMessageHandler(mediator);
        plugin.ConnectionManager = new SignalRConnectionManager(
            request.ServerEndpoint,
            downlinkHandler,
            logger.ForContext<SignalRConnectionManager>());

        // Initialize the connection with the station ID and current callsign
        await plugin.ConnectionManager.InitializeAsync(request.StationId, Network.Callsign);

        // Start the connection
        await plugin.ConnectionManager.StartAsync();

        logger.Information("Successfully connected to ACARS server");
        await mediator.Publish(new ConnectedNotification(request.StationId), cancellationToken);
    }
}
