using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;

namespace CPDLCServer.Tests.Handlers;

public class GetConnectedAircraftRequestHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllAircraftForStation()
    {
        // Arrange
        var aircraftManager = new TestAircraftRepository();
        var handler = new GetConnectedAircraftRequestHandler(aircraftManager);

        // Add test aircraft
        var aircraft1 = new AircraftConnection(
            "UAL123",
            "YBBB",
            "VATSIM",
            DataAuthorityState.CurrentDataAuthority);
        aircraft1.RequestLogon(DateTimeOffset.UtcNow);
        aircraft1.AcceptLogon(DateTimeOffset.UtcNow);
        await aircraftManager.Add(aircraft1, CancellationToken.None);

        var aircraft2 = new AircraftConnection(
            "QFA456",
            "YBBB",
            "VATSIM",
            DataAuthorityState.CurrentDataAuthority);
        aircraft2.RequestLogon(DateTimeOffset.UtcNow);
        await aircraftManager.Add(aircraft2, CancellationToken.None);

        // Different station - should not be returned
        var aircraft3 = new AircraftConnection(
            "AAL789",
            "YMMM",
            "VATSIM",
            DataAuthorityState.CurrentDataAuthority);
        aircraft3.RequestLogon(DateTimeOffset.UtcNow);
        await aircraftManager.Add(aircraft3, CancellationToken.None);

        var query = new GetConnectedAircraftRequest("VATSIM", "YBBB");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Aircraft.Length);
        Assert.Contains(result.Aircraft, a => a.Callsign == "UAL123");
        Assert.Contains(result.Aircraft, a => a.Callsign == "QFA456");
        Assert.DoesNotContain(result.Aircraft, a => a.Callsign == "AAL789");
    }

    [Fact]
    public async Task Handle_ReturnsEmptyArrayWhenNoAircraft()
    {
        // Arrange
        var aircraftManager = new TestAircraftRepository();
        var handler = new GetConnectedAircraftRequestHandler(aircraftManager);

        var query = new GetConnectedAircraftRequest("VATSIM", "YBBB");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Aircraft);
    }

    [Fact]
    public async Task Handle_IncludesAllConnectionState()
    {
        // Arrange
        var aircraftManager = new TestAircraftRepository();
        var handler = new GetConnectedAircraftRequestHandler(aircraftManager);

        var aircraft = new AircraftConnection(
            "UAL123",
            "YBBB",
            "VATSIM",
            DataAuthorityState.CurrentDataAuthority);
        aircraft.RequestLogon(DateTimeOffset.UtcNow);
        aircraft.AcceptLogon(DateTimeOffset.UtcNow);
        await aircraftManager.Add(aircraft, CancellationToken.None);

        var query = new GetConnectedAircraftRequest("VATSIM", "YBBB");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        var aircraftInfo = Assert.Single(result.Aircraft);
        Assert.Equal("UAL123", aircraftInfo.Callsign);
        Assert.Equal("YBBB", aircraftInfo.StationId);
        Assert.Equal("VATSIM", aircraftInfo.FlightSimulationNetwork);
        Assert.Equal(Contracts.DataAuthorityState.CurrentDataAuthority, aircraftInfo.DataAuthorityState);
    }
}
