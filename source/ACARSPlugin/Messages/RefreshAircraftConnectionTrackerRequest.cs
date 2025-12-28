using ACARSPlugin.Model;
using ACARSPlugin.Services;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Serilog;

namespace ACARSPlugin.Messages;

public record RefreshAircraftConnectionTrackerRequest : IRequest;

public class RefreshAircraftConnectionTrackerRequestHandler(
    Plugin plugin,
    AircraftConnectionTracker tracker,
    ILogger logger)
    : IRequestHandler<RefreshAircraftConnectionTrackerRequest>
{
    public async Task Handle(RefreshAircraftConnectionTrackerRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Refreshing aircraft connection tracker");

        if (plugin.ConnectionManager is null)
        {
            logger.Information("No connection manager available, skipping refresh");
            return;
        }

        var connectedAircraft = await plugin.ConnectionManager.GetConnectedAircraft(cancellationToken);

        await tracker.Populate(
            connectedAircraft.Select(c => new AircraftConnection(c.Callsign, c.DataAuthorityState)).ToArray(),
            cancellationToken);

        WeakReferenceMessenger.Default.Send(new ConnectedAircraftChanged());
    }
}