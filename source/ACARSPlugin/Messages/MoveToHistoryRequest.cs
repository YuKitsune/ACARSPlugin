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
        foreach (var group in await repository.GetCurrentDialogueGroups())
        {
            foreach (var dialogue in group.Dialogues)
            {
                if (!dialogue.Messages.Select(x => x.Id).Contains(request.MessageId))
                    continue;

                await repository.MoveToHistory(dialogue);
                await publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
                return;
            }
        }
    }
}