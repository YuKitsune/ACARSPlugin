using CPDLCServer.Model;

namespace CPDLCServer.Persistence;

public interface IAircraftRepository
{
    Task Add(AircraftConnection connection, CancellationToken cancellationToken);
    Task<AircraftConnection?> Find(string flightSimulationNetwork, string stationId, string callsign, CancellationToken cancellationToken);
    Task<AircraftConnection[]> All(string flightSimulationNetwork, string stationId, CancellationToken cancellationToken);
    Task<bool> Remove(string flightSimulationNetwork, string stationId, string callsign, CancellationToken cancellationToken);
}