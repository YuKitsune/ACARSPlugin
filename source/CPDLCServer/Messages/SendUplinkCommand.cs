using CPDLCServer.Model;
using MediatR;

namespace CPDLCServer.Messages;

public record SendUplinkCommand(
    string Sender,
    string FlightSimulationNetwork,
    string StationIdentifier,
    string Recipient,
    int? ReplyToDownlinkId,
    CpdlcUplinkResponseType ResponseType,
    string Content)
    : IRequest<SendUplinkResult>;

public record SendUplinkResult(UplinkMessage UplinkMessage);