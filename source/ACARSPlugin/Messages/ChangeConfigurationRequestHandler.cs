using ACARSPlugin.Configuration;
using MediatR;

namespace ACARSPlugin.Messages;

public record ChangeConfigurationRequest(string ServerEndpoint, string StationIdentifier) : IRequest;

public class ChangeConfigurationRequestHandler(ServerConfiguration serverConfiguration) : IRequestHandler<ChangeConfigurationRequest>
{
    public Task Handle(ChangeConfigurationRequest request, CancellationToken cancellationToken)
    {
        serverConfiguration.ServerEndpoint = request.ServerEndpoint;
        serverConfiguration.StationId = request.StationIdentifier;

        return Task.CompletedTask;
    }
}