using ACARSPlugin.Messages;
using ACARSPlugin.Server.Contracts;
using MediatR;

namespace ACARSPlugin.Server;

public class MediatorMessageHandler(IMediator mediator) : IDownlinkHandlerDelegate
{
    public async Task DownlinkReceived(IDownlinkMessage downlink, CancellationToken cancellationToken)
    {
        await mediator.Publish(new DownlinkMessageReceivedNotification(downlink), cancellationToken);
    }
}