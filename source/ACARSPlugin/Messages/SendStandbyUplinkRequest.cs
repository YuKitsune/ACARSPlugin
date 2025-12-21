using ACARSPlugin.Model;
using ACARSPlugin.Server.Contracts;
using MediatR;

namespace ACARSPlugin.Messages;

public record SendStandbyUplinkRequest(int DownlinkMessageId, string Recipient) : IRequest;
public record SendDeferredUplinkRequest(int DownlinkMessageId, string Recipient) : IRequest;
public record SendUnableUplinkRequest(int DownlinkMessageId, string Recipient, string Reason = "") : IRequest;

public class SendStandbyUplinkRequestHandler(IMediator mediator)
    : IRequestHandler<SendStandbyUplinkRequest>
{
    public async Task Handle(SendStandbyUplinkRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new SendUplinkRequest(
                request.Recipient,
                request.DownlinkMessageId,
                CpdlcUplinkResponseType.NoResponse, // TODO: Is this the correct response type?
                "STANDBY"),
            cancellationToken);
    }
}

public class SendDeferredUplinkRequestHandler(IMediator mediator)
    : IRequestHandler<SendDeferredUplinkRequest>
{
    public async Task Handle(SendDeferredUplinkRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new SendUplinkRequest(
                request.Recipient,
                request.DownlinkMessageId,
                CpdlcUplinkResponseType.NoResponse, // TODO: Is this the correct response type?
                "REQUEST DEFERRED"),
            cancellationToken);
    }
}

public class SendUnableUplinkRequestHandler(IMediator mediator)
    : IRequestHandler<SendUnableUplinkRequest>
{
    public async Task Handle(SendUnableUplinkRequest request, CancellationToken cancellationToken)
    {
        var content = "UNABLE";
        if (!string.IsNullOrEmpty(request.Reason))
        {
            content = "UNABLE. " + request.Reason;
        }

        await mediator.Send(
            new SendUplinkRequest(
                request.Recipient,
                request.DownlinkMessageId,
                CpdlcUplinkResponseType.NoResponse, // TODO: Is this the correct response type?
                content),
            cancellationToken);
    }
}