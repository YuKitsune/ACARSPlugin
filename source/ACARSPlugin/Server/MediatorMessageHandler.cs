using ACARSPlugin.Server.Contracts;
using MediatR;

namespace ACARSPlugin.Server;

public class MediatorMessageHandler(IMediator mediator) : IDownlinkHandlerDelegate
{
    readonly IMediator _mediator = mediator;

    public Task DownlinkReceived(IDownlinkMessage downlink, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}