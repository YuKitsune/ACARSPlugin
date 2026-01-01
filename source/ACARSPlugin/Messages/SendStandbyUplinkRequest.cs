using ACARSPlugin.Server.Contracts;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record SendStandbyUplinkRequest(int DownlinkMessageId, string Recipient) : IRequest;
public record SendDeferredUplinkRequest(int DownlinkMessageId, string Recipient) : IRequest;
public record SendUnableUplinkRequest(int DownlinkMessageId, string Recipient, string Reason = "") : IRequest;

public class SendStandbyUplinkRequestHandler(IMediator mediator, ILogger logger)
    : IRequestHandler<SendStandbyUplinkRequest>
{
    public async Task Handle(SendStandbyUplinkRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Sending STANDBY response to {Recipient} for downlink {DownlinkMessageId}",
            request.Recipient, request.DownlinkMessageId);

        await mediator.Send(
            new SendUplinkRequest(
                request.Recipient,
                request.DownlinkMessageId,
                CpdlcUplinkResponseType.NoResponse, // TODO: Is this the correct response type?
                "STANDBY"),
            cancellationToken);
    }
}

public class SendDeferredUplinkRequestHandler(IMediator mediator, ILogger logger)
    : IRequestHandler<SendDeferredUplinkRequest>
{
    public async Task Handle(SendDeferredUplinkRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Sending REQUEST DEFERRED response to {Recipient} for downlink {DownlinkMessageId}",
            request.Recipient, request.DownlinkMessageId);

        await mediator.Send(
            new SendUplinkRequest(
                request.Recipient,
                request.DownlinkMessageId,
                CpdlcUplinkResponseType.NoResponse, // TODO: Is this the correct response type?
                "REQUEST DEFERRED"),
            cancellationToken);
    }
}

public class SendUnableUplinkRequestHandler(IMediator mediator, ILogger logger)
    : IRequestHandler<SendUnableUplinkRequest>
{
    public async Task Handle(SendUnableUplinkRequest request, CancellationToken cancellationToken)
    {
        var content = "UNABLE";
        if (!string.IsNullOrEmpty(request.Reason))
        {
            content = "UNABLE. " + request.Reason;
        }

        logger.Information("Sending UNABLE response to {Recipient} for downlink {DownlinkMessageId} (Reason: {Reason})",
            request.Recipient, request.DownlinkMessageId, request.Reason ?? "none");

        await mediator.Send(
            new SendUplinkRequest(
                request.Recipient,
                request.DownlinkMessageId,
                CpdlcUplinkResponseType.NoResponse, // TODO: Is this the correct response type?
                content),
            cancellationToken);
    }
}
