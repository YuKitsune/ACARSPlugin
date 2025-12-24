using ACARSPlugin.Model;
using MediatR;

namespace ACARSPlugin.Messages;

public record GetCurrentDialoguesRequest : IRequest<GetCurrentDialoguesResponse>;
public record GetCurrentDialoguesResponse(IReadOnlyList<Dialogue> Dialogues);


public class GetCurrentDialoguesRequestHandler(MessageRepository messageRepository)
    : IRequestHandler<GetCurrentDialoguesRequest, GetCurrentDialoguesResponse>
{
    public async Task<GetCurrentDialoguesResponse> Handle(GetCurrentDialoguesRequest request, CancellationToken cancellationToken)
    {
        var groups = await messageRepository.GetCurrentDialogues();

        // Filter to only include dialogues for the current controller
        var filteredGroups = groups
            .Where(dialogue => Plugin.ShouldDisplayMessage(dialogue.Callsign))
            .ToList();

        return new GetCurrentDialoguesResponse(filteredGroups);
    }
}