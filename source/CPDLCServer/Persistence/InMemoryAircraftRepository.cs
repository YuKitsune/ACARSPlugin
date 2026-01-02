using CPDLCServer.Extensions;
using CPDLCServer.Model;

namespace CPDLCServer.Persistence;

public class InMemoryAircraftRepository : IAircraftRepository
{
    record Key(string FlightSimulationNetwork, string StationId, string Callsign);
 
    readonly SemaphoreSlim _semaphore = new(1, 1);
    
    private readonly Dictionary<Key, AircraftConnection> _connections = new();

    public async Task Add(AircraftConnection connection, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            var key = new Key(connection.FlightSimulationNetwork, connection.StationId, connection.Callsign);
            _connections[key] = connection;
        }
    }
    
    public async Task<AircraftConnection?> Find(string flightSimulationNetwork, string stationId, string callsign, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            var key = new Key(flightSimulationNetwork, stationId, callsign);
            _connections.TryGetValue(key, out var connection);
            return connection;
        }
    }

    public async Task<AircraftConnection[]> All(string flightSimulationNetwork, string stationId, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _connections
                .Where(kvp => kvp.Key.FlightSimulationNetwork == flightSimulationNetwork && kvp.Key.StationId == stationId)
                .Select(kvp => kvp.Value)
                .ToArray();
        }
    }

    public async Task<bool> Remove(string flightSimulationNetwork, string stationId, string callsign, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            var key = new Key(flightSimulationNetwork, stationId, callsign);
            return _connections.Remove(key);
        }
    }
}