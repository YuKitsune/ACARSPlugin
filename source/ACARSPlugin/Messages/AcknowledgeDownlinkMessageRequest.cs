using ACARSPlugin.Model;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record AcknowledgeDownlinkMessageRequest(int MessageId) : IRequest;

public class AcknowledgeDownlinkMessageRequestHandler(MessageRepository repository, IPublisher publisher, ILogger logger)
    : IRequestHandler<AcknowledgeDownlinkMessageRequest>
{
    public async Task Handle(AcknowledgeDownlinkMessageRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Acknowledging downlink message {MessageId}", request.MessageId);
        await repository.AcknowledgeDownlink(request.MessageId);
        await publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
    }
}