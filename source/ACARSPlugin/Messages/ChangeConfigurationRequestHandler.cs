using ACARSPlugin.Configuration;
using MediatR;

namespace ACARSPlugin.Messages;

public record ChangeConfigurationRequest(string ServerEndpoint, string ApiKey, string StationIdentifier) : IRequest;

public class ChangeConfigurationRequestHandler(Plugin plugin) : IRequestHandler<ChangeConfigurationRequest>
{
    public Task Handle(ChangeConfigurationRequest request, CancellationToken cancellationToken)
    {
        ConfigurationStorage.Save(request.ServerEndpoint, request.ApiKey, request.StationIdentifier);
        plugin.UpdateConfiguration(request.ServerEndpoint, request.ApiKey, request.StationIdentifier);
        return Task.CompletedTask;
    }
}