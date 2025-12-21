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
    private readonly IErrorReporter _errorReporter;

    [ObservableProperty] string serverEndpoint;
    [ObservableProperty] string stationIdentifier;
    [ObservableProperty] bool connected;

    public SetupViewModel(IMediator mediator, IErrorReporter errorReporter, string serverEndpoint, string stationIdentifier, bool connected)
    {
        _mediator = mediator;
        _errorReporter = errorReporter;
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
            _errorReporter.ReportError(e);
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
            _errorReporter.ReportError(e);
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