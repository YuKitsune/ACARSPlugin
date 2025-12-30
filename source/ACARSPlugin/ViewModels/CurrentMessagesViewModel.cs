using System.Collections.ObjectModel;
using ACARSPlugin.Configuration;
using ACARSPlugin.Messages;
using ACARSPlugin.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;

namespace ACARSPlugin.ViewModels;

// TODO: Extended view.

public partial class CurrentMessagesViewModel : ObservableObject, IRecipient<CurrentMessagesChanged>, IDisposable
{
    readonly AcarsConfiguration _configuration;
    readonly IMediator _mediator;
    readonly IGuiInvoker _guiInvoker;
    readonly IErrorReporter _errorReporter;
    bool _disposed;

    public CurrentMessagesViewModel(AcarsConfiguration configuration, IMediator mediator, IGuiInvoker guiInvoker, IErrorReporter errorReporter)
    {
        _configuration = configuration;
        _mediator = mediator;
        _guiInvoker = guiInvoker;
        _errorReporter = errorReporter;

        WeakReferenceMessenger.Default.Register(this);

        // Initial load
        _ = LoadDialoguesAsync();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        WeakReferenceMessenger.Default.Unregister<CurrentMessagesChanged>(this);
        _disposed = true;
    }

#if DEBUG
    // Test constructor for design-time data - shows all possible message states
    public CurrentMessagesViewModel()
    {
        var currentMessagesConfiguration = new CurrentMessagesConfiguration();
        var testGroups = new ObservableCollection<DialogueViewModel>();
        var messageId = 1;

        // Group 1: Regular Uplink/Downlink messages (not acknowledged)
        var group1 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-48),
            Messages = new ObservableCollection<CurrentMessageViewModel>()
        };

        group1.Messages.Add(new CurrentMessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST1", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
                "UPLINK REGULAR NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-48), false),
            currentMessagesConfiguration));

        group1.Messages.Add(new CurrentMessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST1", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
                "DOWNLINK REGULAR NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-47), false),
            currentMessagesConfiguration));

        testGroups.Add(group1);

        // Group 3: Urgent messages (not acknowledged)
        var group3 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-45),
            Messages = new ObservableCollection<CurrentMessageViewModel>()
        };

        group3.Messages.Add(new CurrentMessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST3", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
                "UPLINK URGENT NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-45), false) { IsUrgent = true },
            currentMessagesConfiguration));

        group3.Messages.Add(new CurrentMessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST3", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
                "DOWNLINK URGENT NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-44), false) { IsUrgent = true },
            currentMessagesConfiguration));

        testGroups.Add(group3);

        // Group 5: Closed messages (not acknowledged)
        var group5 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-42),
            Messages = new ObservableCollection<CurrentMessageViewModel>()
        };

        group5.Messages.Add(new CurrentMessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST5", Server.Contracts.CpdlcUplinkResponseType.Roger,
                "UPLINK CLOSED NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-42), false) { IsClosed = true },
            currentMessagesConfiguration));

        group5.Messages.Add(new CurrentMessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST5", Server.Contracts.CpdlcDownlinkResponseType.NoResponse,
                "DOWNLINK CLOSED NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-41), false) { IsClosed = true },
            currentMessagesConfiguration));

        testGroups.Add(group5);

        // Group 7: Special Closed messages (not acknowledged)
        var group7 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-39),
            Messages = new ObservableCollection<CurrentMessageViewModel>()
        };

        group7.Messages.Add(new CurrentMessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST7", Server.Contracts.CpdlcUplinkResponseType.Roger,
                "UPLINK SPECIAL CLOSED NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-39), true) { IsClosed = true },
            currentMessagesConfiguration));

        testGroups.Add(group7);

        // Group 9: Failed messages (not acknowledged)
        var group9 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-37),
            Messages = new ObservableCollection<CurrentMessageViewModel>()
        };

        group9.Messages.Add(new CurrentMessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST9", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
                "UPLINK FAILED NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-37), false) { IsTransmissionFailed = true },
            currentMessagesConfiguration));

        testGroups.Add(group9);

        // Group 11: Pilot Late messages (not acknowledged)
        var group11 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-35),
            Messages = new ObservableCollection<CurrentMessageViewModel>()
        };

        group11.Messages.Add(new CurrentMessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST11", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
                "UPLINK PILOT LATE NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-35), false) { IsPilotLate = true },
            currentMessagesConfiguration));

        testGroups.Add(group11);

        // Group 13: Controller Late messages (not acknowledged)
        var group13 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-33),
            Messages = new ObservableCollection<CurrentMessageViewModel>()
        };

        group13.Messages.Add(new CurrentMessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST13", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
                "DOWNLINK CONTROLLER LATE NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-33), false) { IsControllerLate = true },
            currentMessagesConfiguration));

        testGroups.Add(group13);

        // Group 15: Special Closed Timeout (pilot late special - Normal video)
        var group15 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-31),
            Messages = new ObservableCollection<CurrentMessageViewModel>()
        };

        group15.Messages.Add(new CurrentMessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST15", Server.Contracts.CpdlcUplinkResponseType.Roger,
                "UPLINK SPECIAL TIMEOUT NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-31), true) { IsPilotLate = true, IsClosed = true },
            currentMessagesConfiguration));

        testGroups.Add(group15);

        // Group 16: Overflow message (shows asterisk prefix)
        var group16 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-29),
            Messages = new ObservableCollection<CurrentMessageViewModel>()
        };

        group16.Messages.Add(new CurrentMessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST16", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
                "DOWNLINK OVERFLOW MESSAGE SHOWING ASTERISK PREFIX NOT ACKNOWLEDGED VERY LONG TEXT THAT EXCEEDS MAX LENGTH",
                DateTimeOffset.UtcNow.AddMinutes(-29), false),
            currentMessagesConfiguration));

        testGroups.Add(group16);

        // Group 2: Regular messages (acknowledged)
        var group2 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-27),
            Messages = new ObservableCollection<CurrentMessageViewModel>()
        };

        group2.Messages.Add(new CurrentMessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST2", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
                "UPLINK REGULAR ACK", DateTimeOffset.UtcNow.AddMinutes(-27), false) { IsAcknowledged = true },
            currentMessagesConfiguration));

        group2.Messages.Add(new CurrentMessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST2", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
                "DOWNLINK REGULAR ACK", DateTimeOffset.UtcNow.AddMinutes(-26), false) { IsAcknowledged = true },
            currentMessagesConfiguration));

        testGroups.Add(group2);

        // Group 4: Urgent messages (acknowledged)
        var group4 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-24),
            Messages = new ObservableCollection<CurrentMessageViewModel>()
        };

        group4.Messages.Add(new CurrentMessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST4", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
                "UPLINK URGENT ACK", DateTimeOffset.UtcNow.AddMinutes(-24), false) { IsUrgent = true, IsAcknowledged = true },
            currentMessagesConfiguration));

        group4.Messages.Add(new CurrentMessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST4", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
                "DOWNLINK URGENT ACK", DateTimeOffset.UtcNow.AddMinutes(-23), false) { IsUrgent = true, IsAcknowledged = true },
            currentMessagesConfiguration));

        testGroups.Add(group4);

        // Group 6: Closed messages (acknowledged)
        var group6 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-21),
            Messages = new ObservableCollection<CurrentMessageViewModel>()
        };

        group6.Messages.Add(new CurrentMessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST6", Server.Contracts.CpdlcUplinkResponseType.Roger,
                "UPLINK CLOSED ACK", DateTimeOffset.UtcNow.AddMinutes(-21), false) { IsClosed = true, IsAcknowledged = true },
            currentMessagesConfiguration));

        group6.Messages.Add(new CurrentMessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST6", Server.Contracts.CpdlcDownlinkResponseType.NoResponse,
                "DOWNLINK CLOSED ACK", DateTimeOffset.UtcNow.AddMinutes(-20), false) { IsClosed = true, IsAcknowledged = true },
            currentMessagesConfiguration));

        testGroups.Add(group6);

        // Group 8: Special Closed messages (acknowledged)
        var group8 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-18),
            Messages = new ObservableCollection<CurrentMessageViewModel>()
        };

        group8.Messages.Add(new CurrentMessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST8", Server.Contracts.CpdlcUplinkResponseType.Roger,
                "UPLINK SPECIAL CLOSED ACK", DateTimeOffset.UtcNow.AddMinutes(-18), true) { IsClosed = true, IsAcknowledged = true },
            currentMessagesConfiguration));

        testGroups.Add(group8);

        // Group 10: Failed messages (acknowledged)
        var group10 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-16),
            Messages = new ObservableCollection<CurrentMessageViewModel>()
        };

        group10.Messages.Add(new CurrentMessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST10", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
                "UPLINK FAILED ACK", DateTimeOffset.UtcNow.AddMinutes(-16), false) { IsTransmissionFailed = true, IsAcknowledged = true },
            currentMessagesConfiguration));

        testGroups.Add(group10);

        // Group 12: Pilot Late messages (acknowledged)
        var group12 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-14),
            Messages = new ObservableCollection<CurrentMessageViewModel>()
        };

        group12.Messages.Add(new CurrentMessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST12", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
                "UPLINK PILOT LATE ACK", DateTimeOffset.UtcNow.AddMinutes(-14), false) { IsPilotLate = true, IsAcknowledged = true },
            currentMessagesConfiguration));

        testGroups.Add(group12);

        // Group 14: Controller Late messages (acknowledged)
        var group14 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-12),
            Messages = new ObservableCollection<CurrentMessageViewModel>()
        };

        group14.Messages.Add(new CurrentMessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST14", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
                "DOWNLINK CONTROLLER LATE ACK", DateTimeOffset.UtcNow.AddMinutes(-12), false) { IsControllerLate = true, IsAcknowledged = true },
            currentMessagesConfiguration));

        testGroups.Add(group14);

        Dialogues = testGroups;
    }
#endif

    [ObservableProperty]
    ObservableCollection<DialogueViewModel> dialogues = [];

    [ObservableProperty]
    CurrentMessageViewModel? currentlyExtendedMessage;

    async Task LoadDialoguesAsync()
    {
        var response = await _mediator.Send(new GetCurrentDialoguesRequest());

        Dialogues.Clear();
        var dialogueViewModels = response.Dialogues
            .Select(d => new DialogueViewModel
            {
                Messages = new ObservableCollection<CurrentMessageViewModel>(d.Messages.Select(m =>
                    new CurrentMessageViewModel(m, _configuration.CurrentMessages))),
                FirstMessageTime = d.Messages.OrderBy(m => m.Time).First().Time
            });

        foreach (var dialogueViewModel in dialogueViewModels)
        {
            Dialogues.Add(dialogueViewModel);
        }
    }

    public void Receive(CurrentMessagesChanged message)
    {
        if (_disposed)
            return;

        _guiInvoker.InvokeOnGUI(async _ =>
        {
            if (_disposed)
                return;

            try
            {
                await LoadDialoguesAsync();
            }
            catch (Exception ex)
            {
                _errorReporter.ReportError(ex);
            }
        });
    }

    [RelayCommand]
    async Task SendStandby(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            if (currentMessageViewModel.OriginalMessage is not DownlinkMessage downlink)
                return;

            await _mediator.Send(new SendStandbyUplinkRequest(downlink.Id, downlink.Sender));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task SendDeferred(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            if (currentMessageViewModel.OriginalMessage is not DownlinkMessage downlink)
                return;

            await _mediator.Send(new SendDeferredUplinkRequest(downlink.Id, downlink.Sender));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task SendUnable(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            if (currentMessageViewModel.OriginalMessage is not DownlinkMessage downlink)
                return;

            await _mediator.Send(new SendUnableUplinkRequest(downlink.Id, downlink.Sender));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task SendUnableDueTraffic(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            if (currentMessageViewModel.OriginalMessage is not DownlinkMessage downlink)
                return;

            await _mediator.Send(new SendUnableUplinkRequest(downlink.Id, downlink.Sender, Reason: "DUE TO TRAFFIC"));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task SendUnableDueAirspace(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            if (currentMessageViewModel.OriginalMessage is not DownlinkMessage downlink)
                return;

            await _mediator.Send(new SendUnableUplinkRequest(downlink.Id, downlink.Sender, Reason: "DUE TO AIRSPACE RESTRICTION"));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task AcknowledgeDownlink(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            await _mediator.Send(new AcknowledgeDownlinkMessageRequest(currentMessageViewModel.Callsign, currentMessageViewModel.OriginalMessage.Id));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task AcknowledgeUplink(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            await _mediator.Send(new AcknowledgeUplinkMessageRequest(currentMessageViewModel.Callsign, currentMessageViewModel.OriginalMessage.Id));
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task MoveToHistory(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            await _mediator.Send(new MoveToHistoryRequest(currentMessageViewModel.OriginalMessage.Id));
            // TODO: Close if there are no more current messages
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task ReissueMessage(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            if (currentMessageViewModel.OriginalMessage is UplinkMessage uplink)
            {
                // TODO: await _mediator.Send(new ReissueMessageRequest(uplink.Id));
            }
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    void ToggleExtendedDisplay(CurrentMessageViewModel currentMessageViewModel)
    {
        try
        {
            // If this message is already extended, collapse it
            if (CurrentlyExtendedMessage == currentMessageViewModel)
            {
                currentMessageViewModel.IsExtended = false;
                CurrentlyExtendedMessage = null;
                return;
            }

            // Collapse previously extended message
            if (CurrentlyExtendedMessage != null)
                CurrentlyExtendedMessage.IsExtended = false;

            // Extend this message
            currentMessageViewModel.IsExtended = true;
            CurrentlyExtendedMessage = currentMessageViewModel;
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, $"Error extending message display for {currentMessageViewModel.OriginalMessage.Id}");
        }
    }
}
