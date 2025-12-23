using ACARSPlugin.Configuration;
using ACARSPlugin.Model;
using MediatR;

namespace ACARSPlugin.Messages;

public record MoveToHistoryRequest(int MessageId) : IRequest;

public class MoveToHistoryRequestHandler(
    MessageRepository repository,
    IPublisher publisher,
    AcarsConfiguration configuration)
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

            // Prune history if needed
            await repository.PruneHistory(configuration.History.MaxHistory);

            // Publish both notifications
            await publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
            await publisher.Publish(new HistoryMessagesChanged(), cancellationToken);
            return;
        }
    }
}
