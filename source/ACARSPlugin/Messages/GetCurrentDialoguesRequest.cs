using ACARSPlugin.Model;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record GetCurrentDialoguesRequest : IRequest<GetCurrentDialoguesResponse>;
public record GetCurrentDialoguesResponse(IReadOnlyList<Dialogue> Dialogues);


public class GetCurrentDialoguesRequestHandler(MessageRepository messageRepository, ILogger logger)
    : IRequestHandler<GetCurrentDialoguesRequest, GetCurrentDialoguesResponse>
{
    public async Task<GetCurrentDialoguesResponse> Handle(GetCurrentDialoguesRequest request, CancellationToken cancellationToken)
    {
        logger.Debug("Retrieving current dialogues");
        var groups = await messageRepository.GetCurrentDialogues();

        // Filter to only include dialogues for the current controller
        var filteredGroups = groups
            .Where(dialogue => Plugin.ShouldDisplayMessage(dialogue.Callsign))
            .ToList();

        logger.Debug("Found {TotalDialogues} total dialogues, {FilteredDialogues} after filtering for current controller",
            groups.Count, filteredGroups.Count);

        return new GetCurrentDialoguesResponse(filteredGroups);
    }
}