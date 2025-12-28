using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record DisconnectRequest : IRequest;

public class DisconnectRequestHandler(Plugin plugin, IPublisher publisher, ILogger logger) : IRequestHandler<DisconnectRequest>
{
    public async Task Handle(DisconnectRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Processing disconnect request");

        if (plugin.ConnectionManager is null)
        {
            logger.Information("No connection manager found, already disconnected");
            return;
        }

        if (plugin.ConnectionManager.IsConnected)
        {
            logger.Information("Stopping active connection");
            await plugin.ConnectionManager.StopAsync();
        }

        plugin.ConnectionManager.Dispose();
        plugin.ConnectionManager = null;

        logger.Information("Successfully disconnected from ACARS server");
        await publisher.Publish(new DisconnectedNotification(), cancellationToken);
    }
}