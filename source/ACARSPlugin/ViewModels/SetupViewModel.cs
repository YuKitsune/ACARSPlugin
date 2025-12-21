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
    [ObservableProperty] string[] availableStationIdentifiers;
    [ObservableProperty] string selectedStationIdentifier;
    [ObservableProperty] bool connected;

    public SetupViewModel(
        IMediator mediator,
        IErrorReporter errorReporter,
        string serverEndpoint,
        string[] availableStationIdentifiers,
        string selectedStationIdentifier,
        bool connected)
    {
        _mediator = mediator;
        _errorReporter = errorReporter;
        ServerEndpoint = serverEndpoint;
        AvailableStationIdentifiers = availableStationIdentifiers;
        SelectedStationIdentifier = selectedStationIdentifier;
        Connected = connected;

        // Register for connection notifications
        WeakReferenceMessenger.Default.Register<ConnectedNotification>(this);
        WeakReferenceMessenger.Default.Register<DisconnectedNotification>(this);
    }

    [RelayCommand]
    async Task ConnectOrDisconnect()
    {
        try
        {
            if (Connected)
            {
                await _mediator.Send(new DisconnectRequest());
            }
            else
            {
                await _mediator.Send(new ChangeConfigurationRequest(ServerEndpoint, SelectedStationIdentifier));
                await _mediator.Send(new ConnectRequest(ServerEndpoint, SelectedStationIdentifier));
            }
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