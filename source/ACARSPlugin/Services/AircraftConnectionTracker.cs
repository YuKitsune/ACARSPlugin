using ACARSPlugin.Model;
using ACARSPlugin.Server.Contracts;
using Serilog;

namespace ACARSPlugin.Services;

/// <summary>
/// Tracks active aircraft connections to the ACARS server.
/// </summary>
public class AircraftConnectionTracker(ILogger logger)
{
    readonly List<AircraftConnection> _connectedAircraft = new();
    readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task Populate(AircraftConnection[] connections, CancellationToken cancellationToken = default)
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

    public async Task RegisterConnection(AircraftConnection connection, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            logger.Information("Registering aircraft connection: {Callsign}", connection.Callsign);
            _connectedAircraft.Add(connection);
            logger.Debug("Total connected aircraft: {Count}", _connectedAircraft.Count);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<AircraftConnection?> GetConnectedAircraft(string callsign, CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Unregisters an aircraft as disconnected.
    /// </summary>
    /// <param name="callsign">The aircraft callsign</param>
    public async Task UnregisterConnection(string callsign, CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Gets all aircraft currently connected to the server.
    /// </summary>
    /// <returns>A read-only collection of connected aircraft callsigns</returns>
    public async Task<IReadOnlyCollection<AircraftConnection>> GetConnectedAircraft(CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Checks if a specific aircraft is connected.
    /// </summary>
    /// <param name="callsign">The aircraft callsign</param>
    /// <returns>True if the aircraft is connected, otherwise false</returns>
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

    /// <summary>
    /// Clears all tracked connections.
    /// </summary>
    public async Task ClearAll(CancellationToken cancellationToken = default)
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
