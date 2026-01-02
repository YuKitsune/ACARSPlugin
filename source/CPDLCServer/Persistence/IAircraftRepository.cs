using CPDLCServer.Model;

namespace CPDLCServer.Persistence;

public interface IAircraftRepository
{
    Task Add(AircraftConnection connection, CancellationToken cancellationToken);
    Task<AircraftConnection?> Find(string callsign, CancellationToken cancellationToken);
    Task<AircraftConnection[]> All(CancellationToken cancellationToken);
    Task<bool> Remove(string callsign, CancellationToken cancellationToken);
}
