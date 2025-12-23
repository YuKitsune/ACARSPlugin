using ACARSPlugin.ViewModels;
using MediatR;

namespace ACARSPlugin.Messages;

public record SuspendMessageRequest(string Callsign, IEnumerable<UplinkMessageElementViewModel> MessageElements) : IRequest;
public record RestoreSuspendedMessageRequest(string Callsign) : IRequest<RestoreSuspendedMessageResult>;
public record RestoreSuspendedMessageResult(IEnumerable<UplinkMessageElementViewModel> MessageElements) : IRequest;