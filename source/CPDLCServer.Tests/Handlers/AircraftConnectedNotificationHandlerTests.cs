using CPDLCServer.Contracts;
using CPDLCServer.Handlers;
using CPDLCServer.Hubs;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Tests.Mocks;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class AircraftConnectedNotificationHandlerTests
{
    [Fact]
    public async Task Handle_NotifiesControllersOnSameNetworkAndStation()
    {
        // Arrange
        var controllerManager = new TestControllerRepository();
        var controller1 = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-1",
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        var controller2 = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-2",
            "VATSIM",
            "YBBB",
            "BN-OCN_CTR",
            "7654321");
        await controllerManager.Add(controller1, CancellationToken.None);
        await controllerManager.Add(controller2, CancellationToken.None);

        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AircraftConnectedNotificationHandler(
            controllerManager,
            hubContext,
            Logger.None);

        var notification = new AircraftConnected(
            "VATSIM",
            "YBBB",
            "UAL123",
            CPDLCServer.Model.DataAuthorityState.NextDataAuthority);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        hubContext.Clients.Received(1).Clients(
            Arg.Is<IReadOnlyList<string>>(ids =>
                ids.Count == 2 &&
                ids.Contains("ConnectionId-1") &&
                ids.Contains("ConnectionId-2")));

        await clientProxy.Received(1).SendCoreAsync(
            "AircraftConnectionUpdated",
            Arg.Is<object[]>(args =>
                args.Length == 1 &&
                ((AircraftConnectionDto)args[0]).Callsign == "UAL123" &&
                ((AircraftConnectionDto)args[0]).DataAuthorityState == CPDLCServer.Contracts.DataAuthorityState.NextDataAuthority),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnlyNotifiesControllersOnMatchingNetwork()
    {
        // Arrange
        var controllerManager = new TestControllerRepository();
        var vatsimController = new ControllerInfo(
            Guid.NewGuid(),
            "conn-vatsim",
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        var ivaoController = new ControllerInfo(
            Guid.NewGuid(),
            "conn-ivao",
            "IVAO",
            "YBBB",
            "BN-TSN_FSS",
            "7654321");
        await controllerManager.Add(vatsimController, CancellationToken.None);
        await controllerManager.Add(ivaoController, CancellationToken.None);

        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AircraftConnectedNotificationHandler(
            controllerManager,
            hubContext,
            Logger.None);

        var notification = new AircraftConnected(
            "VATSIM",
            "YBBB",
            "UAL123",
            CPDLCServer.Model.DataAuthorityState.NextDataAuthority);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - only VATSIM controller should be notified
        hubContext.Clients.Received(1).Clients(
            Arg.Is<IReadOnlyList<string>>(ids =>
                ids.Count == 1 &&
                ids.Contains("conn-vatsim") &&
                !ids.Contains("conn-ivao")));
    }

    [Fact]
    public async Task Handle_DoesNotNotifyWhenNoControllersConnected()
    {
        // Arrange
        var controllerManager = new TestControllerRepository();
        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AircraftConnectedNotificationHandler(
            controllerManager,
            hubContext,
            Logger.None);

        var notification = new AircraftConnected(
            "VATSIM",
            "YBBB",
            "UAL123",
            CPDLCServer.Model.DataAuthorityState.NextDataAuthority);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - no SignalR notification should be sent
        hubContext.Clients.DidNotReceive().Clients(Arg.Any<IReadOnlyList<string>>());
        await clientProxy.DidNotReceive().SendCoreAsync(
            Arg.Any<string>(),
            Arg.Any<object[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IncludesDataAuthorityStateInNotification()
    {
        // Arrange
        var controllerManager = new TestControllerRepository();
        var controller = new ControllerInfo(
            Guid.NewGuid(),
            "ConnectionId-1",
            "VATSIM",
            "YBBB",
            "BN-TSN_FSS",
            "1234567");
        await controllerManager.Add(controller, CancellationToken.None);

        var hubContext = Substitute.For<IHubContext<ControllerHub>>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Clients(Arg.Any<IReadOnlyList<string>>()).Returns(clientProxy);

        var handler = new AircraftConnectedNotificationHandler(
            controllerManager,
            hubContext,
            Logger.None);

        var notification = new AircraftConnected(
            "VATSIM",
            "YBBB",
            "UAL123",
            CPDLCServer.Model.DataAuthorityState.CurrentDataAuthority);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert - notification includes both callsign and data authority state
        await clientProxy.Received(1).SendCoreAsync(
            "AircraftConnectionUpdated",
            Arg.Is<object[]>(args =>
                args.Length == 1 &&
                ((AircraftConnectionDto)args[0]).Callsign == "UAL123" &&
                ((AircraftConnectionDto)args[0]).DataAuthorityState == CPDLCServer.Contracts.DataAuthorityState.CurrentDataAuthority),
            Arg.Any<CancellationToken>());
    }
}
