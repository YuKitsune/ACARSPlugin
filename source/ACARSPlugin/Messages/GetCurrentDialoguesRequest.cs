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
        return new GetCurrentDialoguesResponse(groups);
    }
}