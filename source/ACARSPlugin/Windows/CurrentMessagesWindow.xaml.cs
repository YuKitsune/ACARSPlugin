using System.Windows;
using System.Windows.Input;
using ACARSPlugin.Model;
using ACARSPlugin.Server.Contracts;
using ACARSPlugin.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace ACARSPlugin.Windows;

public partial class CurrentMessagesWindow : Window
{
    private MessageViewModel? _selectedMessage;

    public CurrentMessagesWindow(CurrentMessagesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Wire up button click events
        StandbyButton.Click += (_, _) => ExecuteCommandAndClosePopup(vm => vm.SendStandbyCommand);
        DeferredButton.Click += (_, _) => ExecuteCommandAndClosePopup(vm => vm.SendDeferredCommand);
        UnableButton.Click += (_, _) => ExecuteCommandAndClosePopup(vm => vm.SendUnableCommand);
        UnableTrafficButton.Click += (_, _) => ExecuteCommandAndClosePopup(vm => vm.SendUnableDueTrafficCommand);
        UnableAirspaceButton.Click += (_, _) => ExecuteCommandAndClosePopup(vm => vm.SendUnableDueAirspaceCommand);
        ManualAckButton.Click += (_, _) => ExecuteCommandAndClosePopup(vm => vm.AcknowledgeUplinkCommand);
        ReissueButton.Click += (_, _) => ExecuteCommandAndClosePopup(vm => vm.ReissueMessageCommand);
        HistoryButton.Click += (_, _) => ExecuteCommandAndClosePopup(vm => vm.MoveToHistoryCommand);
    }

    private void Message_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not MessageViewModel messageViewModel ||
            DataContext is not CurrentMessagesViewModel viewModel)
            return;

        // Acknowledge downlink message as read
        if (messageViewModel.IsDownlink)
            viewModel.AcknowledgeDownlinkCommand.Execute(messageViewModel);
    }

    private void Callsign_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not MessageViewModel messageViewModel ||
            DataContext is not CurrentMessagesViewModel viewModel)
            return;

        // Store selected message
        _selectedMessage = messageViewModel;

        // Acknowledge downlink message as read
        if (messageViewModel.IsDownlink)
            viewModel.AcknowledgeDownlinkCommand.Execute(messageViewModel);

        // Show/hide buttons based on message state
        UpdateButtonVisibility(messageViewModel);

        // Show popup if any buttons are visible
        if (HasVisibleButtons())
        {
            ActionPopup.IsOpen = true;
        }
    }

    private void UpdateButtonVisibility(MessageViewModel message)
    {
        // Hide all buttons first
        StandbyButton.Visibility = Visibility.Collapsed;
        DeferredButton.Visibility = Visibility.Collapsed;
        UnableButton.Visibility = Visibility.Collapsed;
        UnableTrafficButton.Visibility = Visibility.Collapsed;
        UnableAirspaceButton.Visibility = Visibility.Collapsed;
        ManualAckButton.Visibility = Visibility.Collapsed;
        ReissueButton.Visibility = Visibility.Collapsed;
        HistoryButton.Visibility = Visibility.Collapsed;

        // Show buttons based on message type and state
        if (message is { IsDownlink: true, OriginalMessage: DownlinkMessage { ResponseType: CpdlcDownlinkResponseType.ResponseRequired } })
        {
            StandbyButton.Visibility = Visibility.Visible;
            DeferredButton.Visibility = Visibility.Visible;
            UnableButton.Visibility = Visibility.Visible;
            UnableTrafficButton.Visibility = Visibility.Visible;
            UnableAirspaceButton.Visibility = Visibility.Visible;
        }
        else if (message is { IsDownlink: false, OriginalMessage: UplinkMessage { IsPilotLate: true } })
        {
            ManualAckButton.Visibility = Visibility.Visible;
            HistoryButton.Visibility = Visibility.Visible;
        }
        else if (message.OriginalMessage is UplinkMessage { IsTransmissionFailed: true })
        {
            HistoryButton.Visibility = Visibility.Visible;
            ReissueButton.Visibility = Visibility.Visible;
        }
    }

    private bool HasVisibleButtons()
    {
        return StandbyButton.Visibility == Visibility.Visible ||
               DeferredButton.Visibility == Visibility.Visible ||
               UnableButton.Visibility == Visibility.Visible ||
               UnableTrafficButton.Visibility == Visibility.Visible ||
               UnableAirspaceButton.Visibility == Visibility.Visible ||
               ManualAckButton.Visibility == Visibility.Visible ||
               ReissueButton.Visibility == Visibility.Visible ||
               HistoryButton.Visibility == Visibility.Visible;
    }

    private void ExecuteCommandAndClosePopup(Func<CurrentMessagesViewModel, IRelayCommand<MessageViewModel>> commandSelector)
    {
        if (_selectedMessage != null && DataContext is CurrentMessagesViewModel viewModel)
        {
            var command = commandSelector(viewModel);
            command.Execute(_selectedMessage);
        }

        ActionPopup.IsOpen = false;
    }

    private void Popup_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
            return;

        ActionPopup.IsOpen = false;
        e.Handled = true;
    }
}
