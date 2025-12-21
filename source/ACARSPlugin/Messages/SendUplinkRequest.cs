using ACARSPlugin.Model;
using ACARSPlugin.Server.Contracts;
using MediatR;

namespace ACARSPlugin.Messages;

public record SendUplinkRequest(
    string Recipient,
    int? ReplyToDownlinkId,
    CpdlcUplinkResponseType ResponseType,
    string Content)
    : IRequest;

public class SendUplinkRequestHandler(Plugin plugin, MessageRepository messageRepository, IPublisher publisher)
    : IRequestHandler<SendUplinkRequest>
{
    public async Task Handle(SendUplinkRequest request, CancellationToken cancellationToken)
    {
        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
            return;

        var uplinkMessage = await plugin.ConnectionManager.SendUplink(
            request.Recipient,
            request.ReplyToDownlinkId,
            request.ResponseType,
            request.Content,
            cancellationToken);

        await messageRepository.AddUplinkMessage(uplinkMessage, cancellationToken);
        await publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
    }
}