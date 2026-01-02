using CPDLCServer.Model;
using CPDLCServer.Persistence;

namespace CPDLCServer.Tests.Mocks;

public class TestAircraftRepository : IAircraftRepository
{
    private readonly InMemoryAircraftRepository _inner = new();

    public Task Add(AircraftConnection connection, CancellationToken cancellationToken)
    {
        return _inner.Add(connection, cancellationToken);
    }

    public Task<AircraftConnection?> Find(string flightSimulationNetwork, string stationId, string callsign, CancellationToken cancellationToken)
    {
        return _inner.Find(flightSimulationNetwork, stationId, callsign, cancellationToken);
    }

    public Task<AircraftConnection[]> All(string flightSimulationNetwork, string stationId, CancellationToken cancellationToken)
    {
        return _inner.All(flightSimulationNetwork, stationId, cancellationToken);
    }

    public Task<bool> Remove(string flightSimulationNetwork, string stationId, string callsign, CancellationToken cancellationToken)
    {
        return _inner.Remove(flightSimulationNetwork, stationId, callsign, cancellationToken);
    }
}
