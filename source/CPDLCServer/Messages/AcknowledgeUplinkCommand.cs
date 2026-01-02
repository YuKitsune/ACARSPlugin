using MediatR;

namespace CPDLCServer.Messages;

public record AcknowledgeUplinkCommand(Guid DialogueId, int UplinkMessageId) : IRequest;