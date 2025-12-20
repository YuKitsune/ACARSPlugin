using ACARSPlugin.Model;
using MediatR;

namespace ACARSPlugin.Messages;

public record AcknowledgeUplinkMessageRequest(int MessageId) : IRequest;

public class AcknowledgeMessageRequestHandler(MessageRepository repository, IPublisher publisher)
    : IRequestHandler<AcknowledgeUplinkMessageRequest>
{
    public async Task Handle(AcknowledgeUplinkMessageRequest request, CancellationToken cancellationToken)
    {
        await repository.ManuallyAcknowledgeUplink(request.MessageId);
        await publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
    }
}