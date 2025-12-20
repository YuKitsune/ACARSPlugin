using ACARSPlugin.Model;
using MediatR;

namespace ACARSPlugin.Messages;

public record MoveToHistoryRequest(int MessageId) : IRequest;

public class MoveToHistoryRequestHandler(MessageRepository repository, IPublisher publisher)
    : IRequestHandler<MoveToHistoryRequest>
{
    public async Task Handle(MoveToHistoryRequest request, CancellationToken cancellationToken)
    {
        // Find the dialogue containing this message
        foreach (var dialogue in await repository.GetCurrentDialogues())
        {
            if (!dialogue.Messages.Select(x => x.Id).Contains(request.MessageId))
                continue;

            dialogue.IsInHistory = true;
            await publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
            return;
        }
    }
}
