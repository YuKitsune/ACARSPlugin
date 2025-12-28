using ACARSPlugin.Model;
using ACARSPlugin.Server.Contracts;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record SendUplinkRequest(
    string Recipient,
    int? ReplyToDownlinkId,
    CpdlcUplinkResponseType ResponseType,
    string Content)
    : IRequest;

public class SendUplinkRequestHandler(Plugin plugin, MessageRepository messageRepository, IPublisher publisher, ILogger logger)
    : IRequestHandler<SendUplinkRequest>
{
    public async Task Handle(SendUplinkRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Sending uplink to {Recipient} (ReplyTo: {ReplyToDownlinkId}, Type: {ResponseType})",
            request.Recipient, request.ReplyToDownlinkId, request.ResponseType);

        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
        {
            logger.Warning("Cannot send uplink: not connected to server");
            return;
        }

        var uplinkMessage = await plugin.ConnectionManager.SendUplink(
            request.Recipient,
            request.ReplyToDownlinkId,
            request.ResponseType,
            request.Content,
            cancellationToken);

        await messageRepository.AddUplinkMessage(uplinkMessage, cancellationToken);
        logger.Debug("Uplink message {UplinkId} added to repository", uplinkMessage.Id);
        await publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
    }
}