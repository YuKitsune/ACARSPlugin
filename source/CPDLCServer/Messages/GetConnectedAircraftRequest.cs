using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCServer.Messages;

public record GetConnectedAircraftRequest : IRequest<GetConnectedAircraftResult>;

public record GetConnectedAircraftResult(AircraftConnectionDto[] Aircraft);
