using ACARSPlugin.Server.Contracts;

namespace ACARSPlugin.Server;

public interface IDownlinkHandlerDelegate
{
    Task DialogueChanged(DialogueDto dialogue, CancellationToken cancellationToken);
    Task AircraftConnectionUpdated(AircraftConnectionDto aircraftConnectionDto, CancellationToken cancellationToken);
    Task AircraftConnectionRemoved(string callsign, CancellationToken cancellationToken);
    void Error(Exception exception);
}
