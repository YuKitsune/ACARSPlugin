using ACARSPlugin.Server.Contracts;

namespace ACARSPlugin.Server;

public interface IDownlinkHandlerDelegate
{
    Task DownlinkReceived(IDownlinkMessage downlink, CancellationToken cancellationToken);
}