using ACARSPlugin.Model;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record MoveToHistoryRequest(int MessageId) : IRequest;

public class MoveToHistoryRequestHandler(
    MessageRepository repository,
    IPublisher publisher,
    ILogger logger)
    : IRequestHandler<MoveToHistoryRequest>
{
    public async Task Handle(MoveToHistoryRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Moving message {MessageId} to history", request.MessageId);

        // Find the dialogue containing this message
        foreach (var dialogue in await repository.GetCurrentDialogues())
        {
            if (!dialogue.Messages.Select(x => x.Id).Contains(request.MessageId))
                continue;

            dialogue.IsInHistory = true;

            // Publish both notifications
            await publisher.Publish(new CurrentMessagesChanged(), cancellationToken);
            await publisher.Publish(new HistoryMessagesChanged(), cancellationToken);
            return;
        }

        logger.Warning("Message {MessageId} not found in current dialogues", request.MessageId);
    }
}
