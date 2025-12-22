using ACARSPlugin.Server.Contracts;

namespace ACARSPlugin.Server;

public interface IDownlinkHandlerDelegate
{
    Task DownlinkReceived(IDownlinkMessage downlink, CancellationToken cancellationToken);
    Task AircraftConnected(ConnectedAircraftInfo connectedAircraftInfo, CancellationToken cancellationToken);
    Task AircraftDisconnected(string callsign, CancellationToken cancellationToken);
    void Error(Exception exception);
}