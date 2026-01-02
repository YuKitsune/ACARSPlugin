using CPDLCServer.Clients;
using CPDLCServer.Exceptions;

namespace CPDLCServer.Tests.Mocks;

public class TestClientManager : IClientManager
{
    private readonly Dictionary<string, IAcarsClient> _clients = new();
    private readonly TestAcarsClient _defaultClient = new();

    public TestClientManager()
    {
        AddClient("VATSIM", "YBBB", _defaultClient);
        AddClient("VATSIM", "YMMM", _defaultClient);
    }

    public void AddClient(string flightSimulationNetwork, string stationIdentifier, IAcarsClient client)
    {
        var key = CreateKey(flightSimulationNetwork, stationIdentifier);
        _clients[key] = client;
    }

    public Task<IAcarsClient> GetAcarsClient(string flightSimulationNetwork, string stationId, CancellationToken cancellationToken)
    {
        var key = CreateKey(flightSimulationNetwork, stationId);
        if (!_clients.TryGetValue(key, out var client))
        {
            throw new ConfigurationNotFoundException(flightSimulationNetwork, stationId);
        }
        return Task.FromResult(client);
    }

    public bool ClientExists(string flightSimulationNetwork, string stationIdentifier)
    {
        return _clients.ContainsKey(CreateKey(flightSimulationNetwork, stationIdentifier));
    }

    private string CreateKey(string flightSimulationNetwork, string stationId) => $"{flightSimulationNetwork}/{stationId}";
}
