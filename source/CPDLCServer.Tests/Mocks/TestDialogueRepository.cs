using CPDLCServer.Model;
using CPDLCServer.Persistence;

namespace CPDLCServer.Tests.Mocks;

public class TestDialogueRepository : IDialogueRepository
{
    private readonly InMemoryDialogueRepository _inner = new();

    public Task Add(Dialogue dialogue, CancellationToken cancellationToken)
    {
        return _inner.Add(dialogue, cancellationToken);
    }

    public Task<Dialogue?> FindDialogueForMessage(
        string flightSimulationNetwork,
        string stationIdentifier,
        string aircraftCallsign,
        int messageId,
        CancellationToken cancellationToken)
    {
        return _inner.FindDialogueForMessage(
            flightSimulationNetwork,
            stationIdentifier,
            aircraftCallsign,
            messageId,
            cancellationToken);
    }

    public Task<Dialogue?> FindById(Guid id, CancellationToken cancellationToken)
    {
        return _inner.FindById(id, cancellationToken);
    }

    public Task<Dialogue[]> All(CancellationToken cancellationToken)
    {
        return _inner.All(cancellationToken);
    }

    public Task<Dialogue[]> AllForStation(
        string flightSimulationNetwork,
        string stationIdentifier,
        CancellationToken cancellationToken)
    {
        return _inner.AllForStation(flightSimulationNetwork, stationIdentifier, cancellationToken);
    }

    public Task Remove(Dialogue dialogue, CancellationToken cancellationToken)
    {
        return _inner.Remove(dialogue, cancellationToken);
    }
}
