using ACARSPlugin.Model;
using ACARSPlugin.Services;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;

namespace ACARSPlugin.Messages;

public record RefreshAircraftConnectionTrackerRequest : IRequest;

public class RefreshAircraftConnectionTrackerRequestHandler(
    Plugin plugin,
    AircraftConnectionTracker tracker)
    : IRequestHandler<RefreshAircraftConnectionTrackerRequest>
{
    public async Task Handle(RefreshAircraftConnectionTrackerRequest request, CancellationToken cancellationToken)
    {
        if (plugin.ConnectionManager is null)
            return;
        
        var connectedAircraft = await plugin.ConnectionManager.GetConnectedAircraft(cancellationToken);
        
        await tracker.Populate(
            connectedAircraft.Select(c => new AircraftConnection(c.Callsign, c.DataAuthorityState)).ToArray(),
            cancellationToken);

        WeakReferenceMessenger.Default.Send(new ConnectedAircraftChanged());
    }
}