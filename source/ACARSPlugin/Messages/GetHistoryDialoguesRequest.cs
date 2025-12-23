using ACARSPlugin.Model;
using MediatR;

namespace ACARSPlugin.Messages;

public record GetHistoryDialoguesRequest(string Callsign) : IRequest<GetHistoryDialoguesResponse>;
public record GetHistoryDialoguesResponse(IReadOnlyList<Dialogue> Dialogues);

public class GetHistoryDialoguesRequestHandler(MessageRepository messageRepository)
    : IRequestHandler<GetHistoryDialoguesRequest, GetHistoryDialoguesResponse>
{
    public async Task<GetHistoryDialoguesResponse> Handle(GetHistoryDialoguesRequest request, CancellationToken cancellationToken)
    {
        var dialogues = await messageRepository.GetHistoryDialoguesFor(request.Callsign);
        return new GetHistoryDialoguesResponse(dialogues);
    }
}
