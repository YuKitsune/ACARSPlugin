using ACARSPlugin.Model;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record GetHistoryDialoguesRequest(string Callsign) : IRequest<GetHistoryDialoguesResponse>;
public record GetHistoryDialoguesResponse(IReadOnlyList<Dialogue> Dialogues);

public class GetHistoryDialoguesRequestHandler(MessageRepository messageRepository, ILogger logger)
    : IRequestHandler<GetHistoryDialoguesRequest, GetHistoryDialoguesResponse>
{
    public async Task<GetHistoryDialoguesResponse> Handle(GetHistoryDialoguesRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Retrieving history dialogues for {Callsign}", request.Callsign);
        var dialogues = await messageRepository.GetHistoryDialoguesFor(request.Callsign);
        logger.Debug("Found {DialogueCount} history dialogues for {Callsign}", dialogues.Count, request.Callsign);
        return new GetHistoryDialoguesResponse(dialogues);
    }
}
