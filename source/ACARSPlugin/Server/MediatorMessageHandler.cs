using ACARSPlugin.Messages;
using ACARSPlugin.Server.Contracts;
using MediatR;
using vatsys;

namespace ACARSPlugin.Server;

public class MediatorMessageHandler(IMediator mediator) : IDownlinkHandlerDelegate
{
    public async Task DownlinkReceived(IDownlinkMessage downlink, CancellationToken cancellationToken)
    {
        await mediator.Publish(new DownlinkMessageReceivedNotification(downlink), cancellationToken);
    }

    public async Task AircraftConnected(ConnectedAircraftInfo connectedAircraftInfo, CancellationToken cancellationToken)
    {
        await mediator.Publish(new AircraftConnectedNotification(connectedAircraftInfo.Callsign, connectedAircraftInfo.DataAuthorityState), cancellationToken);
    }

    public async Task AircraftDisconnected(string callsign, CancellationToken cancellationToken)
    {
        await mediator.Publish(new AircraftDisconnectedNotification(callsign), cancellationToken);
    }

    public void Error(Exception error)
    {
        Errors.Add(error, Plugin.Name);
        mediator.Send(new DisconnectRequest()).GetAwaiter().GetResult();
    }
}