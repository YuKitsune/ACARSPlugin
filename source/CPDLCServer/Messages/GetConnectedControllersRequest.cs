using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCServer.Messages;

public record GetConnectedControllersRequest : IRequest<GetConnectedControllersResult>;

public record GetConnectedControllersResult(ControllerConnectionDto[] Controllers);
