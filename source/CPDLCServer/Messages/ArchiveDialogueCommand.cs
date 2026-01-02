using MediatR;

namespace CPDLCServer.Messages;

public record ArchiveDialogueCommand(Guid DialogueId) : IRequest;
