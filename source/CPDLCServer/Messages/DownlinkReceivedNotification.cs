using CPDLCServer.Model;
using MediatR;

namespace CPDLCServer.Messages;

public record DownlinkReceivedNotification(
    string FlightSimulationNetwork,
    string StationIdentifier,
    DownlinkMessage Downlink)
    : INotification;
