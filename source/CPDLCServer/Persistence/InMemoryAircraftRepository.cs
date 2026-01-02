using CPDLCServer.Extensions;
using CPDLCServer.Model;

namespace CPDLCServer.Persistence;

public class InMemoryAircraftRepository : IAircraftRepository
{
    readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly Dictionary<string, AircraftConnection> _connections = new();

    public async Task Add(AircraftConnection connection, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            _connections[connection.Callsign] = connection;
        }
    }

    public async Task<AircraftConnection?> Find(string callsign, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            _connections.TryGetValue(callsign, out var connection);
            return connection;
        }
    }

    public async Task<AircraftConnection[]> All(CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _connections
                .Select(kvp => kvp.Value)
                .ToArray();
        }
    }

    public async Task<bool> Remove(string callsign, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _connections.Remove(callsign);
        }
    }
}
