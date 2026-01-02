using MediatR;

namespace CPDLCServer.Messages;

public record AcknowledgeDownlinkCommand(Guid DialogueId, int DownlinkMessageId) : IRequest;