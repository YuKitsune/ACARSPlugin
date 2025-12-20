using ACARSPlugin.Model;
using ACARSPlugin.Server.Contracts;
using MediatR;

namespace ACARSPlugin.Messages;

public record SendUplinkRequest(ICpdlcUplink Uplink) : IRequest;

public class SendUplinkRequestHandler(Plugin plugin, MessageRepository messageRepository, IPublisher publisher)
    : IRequestHandler<SendUplinkRequest>
{
    public async Task Handle(SendUplinkRequest request, CancellationToken cancellationToken)
    {
        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
            return;

        await plugin.ConnectionManager.SendUplink(request.Uplink, cancellationToken);
        await messageRepository.AddUplinkMessage(request.Uplink, cancellationToken);
        await publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
    }
}