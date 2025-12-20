using ACARSPlugin.Configuration;
using MediatR;

namespace ACARSPlugin.Messages;

public record ChangeConfigurationRequest(string ServerEndpoint, string ApiKey, string StationIdentifier) : IRequest;

public class ChangeConfigurationRequestHandler(ServerConfiguration serverConfiguration) : IRequestHandler<ChangeConfigurationRequest>
{
    public Task Handle(ChangeConfigurationRequest request, CancellationToken cancellationToken)
    {
        serverConfiguration.ServerEndpoint = request.ServerEndpoint;
        serverConfiguration.ServerApiKey = request.ApiKey;
        serverConfiguration.StationId = request.StationIdentifier;
        
        // Only persist the API key
        ConfigurationStorage.SaveApiKey(request.ApiKey);

        return Task.CompletedTask;
    }
}