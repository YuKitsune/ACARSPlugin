using MediatR;
using vatsys;

namespace ACARSPlugin.Messages;

public record GetTrackingControllerRequest(string AircraftCallsign) : IRequest<GetTrackingControllerResponse>;

public record GetTrackingControllerResponse(string ControllerCallsign);

public class GetTrackingControllerRequestHandler : IRequestHandler<GetTrackingControllerRequest, GetTrackingControllerResponse>
{
    public async Task<GetTrackingControllerResponse> Handle(GetTrackingControllerRequest request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var fdr = FDP2.GetFDRs.FirstOrDefault(f => f.Callsign == request.AircraftCallsign);

        return new GetTrackingControllerResponse(fdr?.ControllerTracking?.Callsign ?? string.Empty);
    }
}
