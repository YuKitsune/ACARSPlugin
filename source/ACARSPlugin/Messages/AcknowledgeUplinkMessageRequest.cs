using ACARSPlugin.Model;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record AcknowledgeUplinkMessageRequest(string Callsign, int MessageId) : IRequest;

public class AcknowledgeMessageRequestHandler(MessageRepository repository, IPublisher publisher, ILogger logger)
    : IRequestHandler<AcknowledgeUplinkMessageRequest>
{
    public async Task Handle(AcknowledgeUplinkMessageRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Manually acknowledging uplink message {MessageId} to {Callsign}", request.MessageId, request.Callsign);
        await repository.ManuallyAcknowledgeUplink(request.Callsign, request.MessageId);
        await publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
    }
}
