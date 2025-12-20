using MediatR;

namespace ACARSPlugin.Messages;

public record DisconnectRequest : IRequest;

public class DisconnectRequestHandler(Plugin plugin, IPublisher publisher) : IRequestHandler<DisconnectRequest>
{
    public async Task Handle(DisconnectRequest request, CancellationToken cancellationToken)
    {
        if (plugin.ConnectionManager is null)
        {
            return;
        }

        if (plugin.ConnectionManager.IsConnected)
        {
            await plugin.ConnectionManager.StopAsync();
        }

        plugin.ConnectionManager.Dispose();
        plugin.ConnectionManager = null;

        await publisher.Publish(new DisconnectedNotification(), cancellationToken);
    }
}