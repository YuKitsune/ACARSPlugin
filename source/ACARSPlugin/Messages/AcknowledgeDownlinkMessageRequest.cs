using ACARSPlugin.Model;
using MediatR;

namespace ACARSPlugin.Messages;

public record AcknowledgeDownlinkMessageRequest(int MessageId) : IRequest;

public class AcknowledgeDownlinkMessageRequestHandler(MessageRepository repository, IPublisher publisher)
    : IRequestHandler<AcknowledgeDownlinkMessageRequest>
{
    public async Task Handle(AcknowledgeDownlinkMessageRequest request, CancellationToken cancellationToken)
    {
        await repository.AcknowledgeDownlink(request.MessageId);
        await publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
    }
}