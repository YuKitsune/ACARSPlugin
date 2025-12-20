using ACARSPlugin.Model;
using ACARSPlugin.Server.Contracts;
using MediatR;

namespace ACARSPlugin.Messages;

public record SendStandbyUplinkRequest(int DownlinkMessageId, string Recipient) : IRequest;
public record SendDeferredUplinkRequest(int DownlinkMessageId, string Recipient) : IRequest;
public record SendUnableUplinkRequest(int DownlinkMessageId, string Recipient, string Reason = "") : IRequest;

public class SendStandbyUplinkRequestHandler(IMessageIdProvider messageIdProvider, IMediator mediator)
    : IRequestHandler<SendStandbyUplinkRequest>
{
    public async Task Handle(SendStandbyUplinkRequest request, CancellationToken cancellationToken)
    {
        var uplink = new CpdlcUplinkReply(
            MessageId: await messageIdProvider.AllocateMessageId(cancellationToken),
            Recipient: request.Recipient,
            ResponseType: CpdlcUplinkResponseType.NoResponse, // TODO: Is this the correct response type?
            Content: "STANDBY",
            ReplyToMessageId: request.DownlinkMessageId);
        await mediator.Send(new SendUplinkRequest(uplink), cancellationToken);
    }
}

public class SendDeferredUplinkRequestHandler(IMessageIdProvider messageIdProvider, IMediator mediator)
    : IRequestHandler<SendDeferredUplinkRequest>
{
    public async Task Handle(SendDeferredUplinkRequest request, CancellationToken cancellationToken)
    {
        var uplink = new CpdlcUplinkReply(
            MessageId: await messageIdProvider.AllocateMessageId(cancellationToken),
            Recipient: request.Recipient,
            ResponseType: CpdlcUplinkResponseType.NoResponse, // TODO: Is this the correct response type?
            Content: "REQUEST DEFERRED",
            ReplyToMessageId: request.DownlinkMessageId);
        await mediator.Send(new SendUplinkRequest(uplink), cancellationToken);
    }
}

public class SendUnableUplinkRequestHandler(IMessageIdProvider messageIdProvider, IMediator mediator)
    : IRequestHandler<SendUnableUplinkRequest>
{
    public async Task Handle(SendUnableUplinkRequest request, CancellationToken cancellationToken)
    {
        var content = "UNABLE";
        if (!string.IsNullOrEmpty(request.Reason))
        {
            content = "UNABLE. " + request.Reason;
        }

        var uplink = new CpdlcUplinkReply(
            MessageId: await messageIdProvider.AllocateMessageId(cancellationToken),
            Recipient: request.Recipient,
            ResponseType: CpdlcUplinkResponseType.NoResponse, // TODO: Is this the correct response type?
            Content: content,
            ReplyToMessageId: request.DownlinkMessageId);
        await mediator.Send(new SendUplinkRequest(uplink), cancellationToken);
    }
}