using ACARSPlugin.Model;
using MediatR;

namespace ACARSPlugin.Messages;

public record GetCurrentDialogueGroupsRequest : IRequest<GetCurrentDialogueGroupsResponse>;
public record GetCurrentDialogueGroupsResponse(IReadOnlyList<DialogueGroup> DialogueGroups);

public class GetCurrentDialogueGroupsRequestHandler(MessageRepository messageRepository)
    : IRequestHandler<GetCurrentDialogueGroupsRequest, GetCurrentDialogueGroupsResponse>
{
    public async Task<GetCurrentDialogueGroupsResponse> Handle(GetCurrentDialogueGroupsRequest request, CancellationToken cancellationToken)
    {
        var groups = await messageRepository.GetCurrentDialogueGroups();
        return new GetCurrentDialogueGroupsResponse(groups);
    }
}