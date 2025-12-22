using ACARSPlugin.Model;

namespace ACARSPlugin.Services;

/// <summary>
/// Tracks active aircraft connections to the ACARS server.
/// </summary>
public class AircraftConnectionTracker
{
    private readonly List<AircraftConnection> _connectedAircraft = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task Populate(AircraftConnection[] connections, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _connectedAircraft.Clear();
            _connectedAircraft.AddRange(connections);
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
            _connectedAircraft.Add(connection);
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
            _connectedAircraft.RemoveAll(c => c.Callsign == callsign);
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
            return _connectedAircraft.Any(c => c.Callsign == callsign);
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
            _connectedAircraft.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
