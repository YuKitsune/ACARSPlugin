using ACARSPlugin.Server;
using MediatR;
using vatsys;

namespace ACARSPlugin.Messages;

public record ConnectRequest(string ServerEndpoint, string StationId) : IRequest;

public class ConnectRequestHandler(Plugin plugin, IMediator mediator, IPublisher publisher) : IRequestHandler<ConnectRequest>
{
    public async Task Handle(ConnectRequest request, CancellationToken cancellationToken)
    {
        // If already connected, disconnect first
        if (plugin.ConnectionManager is not null)
        {
            if (plugin.ConnectionManager.IsConnected)
            {
                await plugin.ConnectionManager.StopAsync();
            }

            plugin.ConnectionManager.Dispose();
            plugin.ConnectionManager = null;
        }

        if (!Network.IsConnected)
        {
            throw new Exception("Not connected to VATSIM");
        }

        var downlinkHandler = new MediatorMessageHandler(mediator);
        plugin.ConnectionManager = new SignalRConnectionManager(
            request.ServerEndpoint,
            downlinkHandler);

        // Initialize the connection with the station ID and current callsign
        await plugin.ConnectionManager.InitializeAsync(request.StationId, Network.Callsign);

        // Start the connection
        await plugin.ConnectionManager.StartAsync();

        await publisher.Publish(new ConnectedNotification(request.StationId), cancellationToken);
    }
}