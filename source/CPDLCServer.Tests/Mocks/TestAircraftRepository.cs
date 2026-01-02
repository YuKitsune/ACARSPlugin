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

    public Task<AircraftConnection?> Find(string callsign, CancellationToken cancellationToken)
    {
        return _inner.Find(callsign, cancellationToken);
    }

    public Task<AircraftConnection[]> All(CancellationToken cancellationToken)
    {
        return _inner.All(cancellationToken);
    }

    public Task<bool> Remove(string callsign, CancellationToken cancellationToken)
    {
        return _inner.Remove(callsign, cancellationToken);
    }
}
