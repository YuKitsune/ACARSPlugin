using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;

namespace ACARSPlugin.Messages;

public record DisconnectRequest : IRequest;

public class DisconnectRequestHandler(Plugin plugin) : IRequestHandler<DisconnectRequest>
{
    public async Task Handle(DisconnectRequest request, CancellationToken cancellationToken)
    {
        if (plugin.ConnectionManager is null)
        {
            return;
        }

        if (plugin.ConnectionManager.IsConnected)
        {
            await plugin.ConnectionManager.StopAsync();
        }

        plugin.ConnectionManager.Dispose();
        plugin.ConnectionManager = null;
        
        WeakReferenceMessenger.Default.Send(new DisconnectedNotification());
    }
}