using ACARSPlugin.Server.Contracts;
using Serilog;

namespace ACARSPlugin;

public class AircraftConnectionStore(ILogger logger)
{
    readonly List<AircraftConnectionDto> _connectedAircraft = new();
    readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task Populate(AircraftConnectionDto[] connections, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            logger.Information("Populating aircraft connection tracker with {ConnectionCount} connections", connections.Length);
            _connectedAircraft.Clear();
            _connectedAircraft.AddRange(connections);
            logger.Debug("Aircraft connections populated: {Callsigns}", string.Join(", ", connections.Select(c => c.Callsign)));
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task Upsert(AircraftConnectionDto connection, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            logger.Information("Upserting aircraft connection: {Callsign}", connection.Callsign);
            var existing = _connectedAircraft.FirstOrDefault(c => c.Callsign == connection.Callsign);
            if (existing is not null)
            {
                _connectedAircraft.Remove(existing);
            }

            _connectedAircraft.Add(connection);
            logger.Debug("Total connected aircraft: {Count}", _connectedAircraft.Count);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<AircraftConnectionDto?> Find(string callsign, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var connection = _connectedAircraft.FirstOrDefault(a => a.Callsign == callsign);
            logger.Debug("Looking up aircraft connection for {Callsign}: {Found}", callsign, connection != null ? "Found" : "Not found");
            return connection;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyCollection<AircraftConnectionDto>> All(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            logger.Debug("Retrieving all connected aircraft: {Count} aircraft", _connectedAircraft.Count);
            return _connectedAircraft.ToArray();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> IsConnected(string callsign, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var isConnected = _connectedAircraft.Any(c => c.Callsign == callsign);
            logger.Debug("Checking if aircraft {Callsign} is connected: {IsConnected}", callsign, isConnected);
            return isConnected;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task Remove(string callsign, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var removedCount = _connectedAircraft.RemoveAll(c => c.Callsign == callsign);
            if (removedCount > 0)
            {
                logger.Information("Unregistered aircraft connection: {Callsign}", callsign);
                logger.Debug("Total connected aircraft: {Count}", _connectedAircraft.Count);
            }
            else
            {
                logger.Warning("Attempted to unregister aircraft {Callsign} but it was not found in the tracker", callsign);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task Clear(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var count = _connectedAircraft.Count;
            logger.Information("Clearing all aircraft connections ({Count} connections)", count);
            _connectedAircraft.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
