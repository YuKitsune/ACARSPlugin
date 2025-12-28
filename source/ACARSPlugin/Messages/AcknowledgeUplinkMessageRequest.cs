using ACARSPlugin.Model;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record AcknowledgeUplinkMessageRequest(int MessageId) : IRequest;

public class AcknowledgeMessageRequestHandler(MessageRepository repository, IPublisher publisher, ILogger logger)
    : IRequestHandler<AcknowledgeUplinkMessageRequest>
{
    public async Task Handle(AcknowledgeUplinkMessageRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Manually acknowledging uplink message {MessageId}", request.MessageId);
        await repository.ManuallyAcknowledgeUplink(request.MessageId);
        await publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
    }
}