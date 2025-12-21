using ACARSPlugin.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using vatsys;

namespace ACARSPlugin.ViewModels;

public partial class SetupViewModel : ObservableObject,
    IRecipient<ConnectedNotification>,
    IRecipient<DisconnectedNotification>
{
    private readonly IMediator _mediator;

    [ObservableProperty] string serverEndpoint;
    [ObservableProperty] string stationIdentifier;
    [ObservableProperty] bool connected;

    public SetupViewModel(IMediator mediator, string serverEndpoint, string stationIdentifier, bool connected)
    {
        _mediator = mediator;
        ServerEndpoint = serverEndpoint;
        StationIdentifier = stationIdentifier;
        Connected = connected;

        // Register for connection notifications
        WeakReferenceMessenger.Default.Register<ConnectedNotification>(this);
        WeakReferenceMessenger.Default.Register<DisconnectedNotification>(this);
    }

    [RelayCommand]
    async Task Connect()
    {
        try
        {
            await _mediator.Send(new ChangeConfigurationRequest(ServerEndpoint, StationIdentifier));
            await _mediator.Send(new ConnectRequest(ServerEndpoint, StationIdentifier));
        }
        catch (Exception e)
        {
            Errors.Add(e, Plugin.Name);
        }
    }

    [RelayCommand]
    async Task Disconnect()
    {
        try
        {
            await _mediator.Send(new DisconnectRequest());
        }
        catch (Exception e)
        {
            Errors.Add(e, Plugin.Name);
        }
    }

    public void Receive(ConnectedNotification message)
    {
        Connected = true;
    }

    public void Receive(DisconnectedNotification message)
    {
        Connected = false;
    }
}