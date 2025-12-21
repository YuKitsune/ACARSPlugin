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

public partial class CurrentMessagesViewModel : ObservableObject, IRecipient<CurrentMessagesChanged>
{
    readonly AcarsConfiguration _configuration;
    readonly IMediator _mediator;
    readonly IGuiInvoker _guiInvoker;

    public CurrentMessagesViewModel(AcarsConfiguration configuration, IMediator mediator, IGuiInvoker guiInvoker)
    {
        _configuration = configuration;
        _mediator = mediator;
        _guiInvoker = guiInvoker;

        WeakReferenceMessenger.Default.Register(this);

        // Initial load
        _ = LoadDialoguesAsync();
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
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-30),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        group1.Messages.Add(new MessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST1", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
                "UPLINK REGULAR NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-30), false),
            currentMessagesConfiguration));

        group1.Messages.Add(new MessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST1", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
                "DOWNLINK REGULAR NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-29), false),
            currentMessagesConfiguration));

        testGroups.Add(group1);

        // Group 2: Regular messages (acknowledged)
        var group2 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-28),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        group2.Messages.Add(new MessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST2", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
                "UPLINK REGULAR ACK", DateTimeOffset.UtcNow.AddMinutes(-28), false) { IsAcknowledged = true },
            currentMessagesConfiguration));

        group2.Messages.Add(new MessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST2", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
                "DOWNLINK REGULAR ACK", DateTimeOffset.UtcNow.AddMinutes(-27), false) { IsAcknowledged = true },
            currentMessagesConfiguration));

        testGroups.Add(group2);

        // Group 3: Urgent messages (not acknowledged)
        var group3 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-25),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        group3.Messages.Add(new MessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST3", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
                "UPLINK URGENT NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-25), false) { IsUrgent = true },
            currentMessagesConfiguration));

        group3.Messages.Add(new MessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST3", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
                "DOWNLINK URGENT NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-24), false) { IsUrgent = true },
            currentMessagesConfiguration));

        testGroups.Add(group3);

        // Group 4: Urgent messages (acknowledged)
        var group4 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-22),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        group4.Messages.Add(new MessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST4", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
                "UPLINK URGENT ACK", DateTimeOffset.UtcNow.AddMinutes(-22), false) { IsUrgent = true, IsAcknowledged = true },
            currentMessagesConfiguration));

        group4.Messages.Add(new MessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST4", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
                "DOWNLINK URGENT ACK", DateTimeOffset.UtcNow.AddMinutes(-21), false) { IsUrgent = true, IsAcknowledged = true },
            currentMessagesConfiguration));

        testGroups.Add(group4);

        // Group 5: Closed messages (not acknowledged)
        var group5 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-19),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        group5.Messages.Add(new MessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST5", Server.Contracts.CpdlcUplinkResponseType.Roger,
                "UPLINK CLOSED NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-19), false) { IsClosed = true },
            currentMessagesConfiguration));

        group5.Messages.Add(new MessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST5", Server.Contracts.CpdlcDownlinkResponseType.NoResponse,
                "DOWNLINK CLOSED NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-18), false) { IsClosed = true },
            currentMessagesConfiguration));

        testGroups.Add(group5);

        // Group 6: Closed messages (acknowledged)
        var group6 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-16),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        group6.Messages.Add(new MessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST6", Server.Contracts.CpdlcUplinkResponseType.Roger,
                "UPLINK CLOSED ACK", DateTimeOffset.UtcNow.AddMinutes(-16), false) { IsClosed = true, IsAcknowledged = true },
            currentMessagesConfiguration));

        group6.Messages.Add(new MessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST6", Server.Contracts.CpdlcDownlinkResponseType.NoResponse,
                "DOWNLINK CLOSED ACK", DateTimeOffset.UtcNow.AddMinutes(-15), false) { IsClosed = true, IsAcknowledged = true },
            currentMessagesConfiguration));

        testGroups.Add(group6);

        // Group 7: Special Closed messages (not acknowledged)
        var group7 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-13),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        group7.Messages.Add(new MessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST7", Server.Contracts.CpdlcUplinkResponseType.Roger,
                "UPLINK SPECIAL CLOSED NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-13), true) { IsClosed = true },
            currentMessagesConfiguration));

        testGroups.Add(group7);

        // Group 8: Special Closed messages (acknowledged)
        var group8 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-11),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        group8.Messages.Add(new MessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST8", Server.Contracts.CpdlcUplinkResponseType.Roger,
                "UPLINK SPECIAL CLOSED ACK", DateTimeOffset.UtcNow.AddMinutes(-11), true) { IsClosed = true, IsAcknowledged = true },
            currentMessagesConfiguration));

        testGroups.Add(group8);

        // Group 9: Failed messages (not acknowledged)
        var group9 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-9),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        group9.Messages.Add(new MessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST9", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
                "UPLINK FAILED NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-9), false) { IsTransmissionFailed = true },
            currentMessagesConfiguration));

        testGroups.Add(group9);

        // Group 10: Failed messages (acknowledged)
        var group10 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-7),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        group10.Messages.Add(new MessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST10", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
                "UPLINK FAILED ACK", DateTimeOffset.UtcNow.AddMinutes(-7), false) { IsTransmissionFailed = true, IsAcknowledged = true },
            currentMessagesConfiguration));

        testGroups.Add(group10);

        // Group 11: Pilot Late messages (not acknowledged)
        var group11 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-5),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        group11.Messages.Add(new MessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST11", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
                "UPLINK PILOT LATE NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-5), false) { IsPilotLate = true },
            currentMessagesConfiguration));

        testGroups.Add(group11);

        // Group 12: Pilot Late messages (acknowledged)
        var group12 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-3),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        group12.Messages.Add(new MessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST12", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
                "UPLINK PILOT LATE ACK", DateTimeOffset.UtcNow.AddMinutes(-3), false) { IsPilotLate = true, IsAcknowledged = true },
            currentMessagesConfiguration));

        testGroups.Add(group12);

        // Group 13: Controller Late messages (not acknowledged)
        var group13 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-2),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        group13.Messages.Add(new MessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST13", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
                "DOWNLINK CONTROLLER LATE NOT ACK", DateTimeOffset.UtcNow.AddMinutes(-2), false) { IsControllerLate = true },
            currentMessagesConfiguration));

        testGroups.Add(group13);

        // Group 14: Controller Late messages (acknowledged)
        var group14 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-1),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        group14.Messages.Add(new MessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST14", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
                "DOWNLINK CONTROLLER LATE ACK", DateTimeOffset.UtcNow.AddMinutes(-1), false) { IsControllerLate = true, IsAcknowledged = true },
            currentMessagesConfiguration));

        testGroups.Add(group14);

        // Group 15: Special Closed Timeout (pilot late special - Normal video)
        var group15 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow,
            Messages = new ObservableCollection<MessageViewModel>()
        };

        group15.Messages.Add(new MessageViewModel(
            new Model.UplinkMessage(messageId++, "TEST15", Server.Contracts.CpdlcUplinkResponseType.Roger,
                "UPLINK SPECIAL TIMEOUT NOT ACK", DateTimeOffset.UtcNow, true) { IsPilotLate = true, IsClosed = true },
            currentMessagesConfiguration));

        testGroups.Add(group15);

        // Group 16: Overflow message (shows asterisk prefix)
        var group16 = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-32),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        group16.Messages.Add(new MessageViewModel(
            new Model.DownlinkMessage(messageId++, "TEST16", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
                "DOWNLINK OVERFLOW MESSAGE SHOWING ASTERISK PREFIX NOT ACKNOWLEDGED VERY LONG TEXT THAT EXCEEDS MAX LENGTH",
                DateTimeOffset.UtcNow.AddMinutes(-32), false),
            currentMessagesConfiguration));

        testGroups.Add(group16);

        Dialogues = testGroups;
    }
#endif

    [ObservableProperty]
    private ObservableCollection<DialogueViewModel> dialogues = [];

    [ObservableProperty]
    private MessageViewModel? currentlyExtendedMessage;

    private async Task LoadDialoguesAsync()
    {
        var response = await _mediator.Send(new GetCurrentDialoguesRequest());

        Dialogues.Clear();
        var dialogueViewModels = response.Dialogues
            .Select(d => new DialogueViewModel
            {
                Messages = new ObservableCollection<MessageViewModel>(d.Messages.Select(m =>
                    new MessageViewModel(m, _configuration.CurrentMessages))),
                FirstMessageTime = d.Messages.OrderBy(m => m.Time).First().Time
            });
        
        foreach (var dialogueViewModel in dialogueViewModels)
        {
            Dialogues.Add(dialogueViewModel);
        }
    }

    public void Receive(CurrentMessagesChanged message)
    {
        _guiInvoker.InvokeOnGUI(async () =>
        {
            try
            {
                await LoadDialoguesAsync();
            }
            catch (Exception ex)
            {
                // TODO: Bubble up
            }
        });
    }

    [RelayCommand]
    private async Task SendStandby(MessageViewModel messageViewModel)
    {
        if (messageViewModel.OriginalMessage is DownlinkMessage downlink)
        {
            await _mediator.Send(new SendStandbyUplinkRequest(downlink.Id, downlink.Sender));
        }
    }

    [RelayCommand]
    private async Task SendDeferred(MessageViewModel messageViewModel)
    {
        if (messageViewModel.OriginalMessage is DownlinkMessage downlink)
        {
            await _mediator.Send(new SendDeferredUplinkRequest(downlink.Id, downlink.Sender));
        }
    }

    [RelayCommand]
    private async Task SendUnable(MessageViewModel messageViewModel)
    {
        if (messageViewModel.OriginalMessage is DownlinkMessage downlink)
        {
            await _mediator.Send(new SendUnableUplinkRequest(downlink.Id, downlink.Sender));
        }
    }

    [RelayCommand]
    private async Task SendUnableDueTraffic(MessageViewModel messageViewModel)
    {
        if (messageViewModel.OriginalMessage is DownlinkMessage downlink)
        {
            await _mediator.Send(new SendUnableUplinkRequest(downlink.Id, downlink.Sender, Reason: "DUE TO TRAFFIC"));
        }
    }

    [RelayCommand]
    private async Task SendUnableDueAirspace(MessageViewModel messageViewModel)
    {
        if (messageViewModel.OriginalMessage is DownlinkMessage downlink)
        {
            await _mediator.Send(new SendUnableUplinkRequest(downlink.Id, downlink.Sender, Reason: "DUE TO AIRSPACE RESTRICTION"));
        }
    }

    [RelayCommand]
    private async Task AcknowledgeDownlink(MessageViewModel messageViewModel)
    {
        await _mediator.Send(new AcknowledgeDownlinkMessageRequest(messageViewModel.OriginalMessage.Id));
    }

    [RelayCommand]
    private async Task AcknowledgeUplink(MessageViewModel messageViewModel)
    {
        await _mediator.Send(new AcknowledgeUplinkMessageRequest(messageViewModel.OriginalMessage.Id));
    }

    [RelayCommand]
    private async Task MoveToHistory(MessageViewModel messageViewModel)
    {
        await _mediator.Send(new MoveToHistoryRequest(messageViewModel.OriginalMessage.Id));
        // TODO: Close if there are no more current messages
    }

    [RelayCommand]
    private async Task ReissueMessage(MessageViewModel messageViewModel)
    {
        if (messageViewModel.OriginalMessage is UplinkMessage uplink)
        {
            // TODO: await _mediator.Send(new ReissueMessageRequest(uplink.Id));
        }
    }

    [RelayCommand]
    private void ToggleExtendedDisplay(MessageViewModel messageViewModel)
    {
        // If this message is already extended, collapse it
        if (CurrentlyExtendedMessage == messageViewModel)
        {
            messageViewModel.IsExtended = false;
            CurrentlyExtendedMessage = null;
            return;
        }

        // Collapse previously extended message
        if (CurrentlyExtendedMessage != null)
        {
            CurrentlyExtendedMessage.IsExtended = false;
        }

        // Extend this message
        messageViewModel.IsExtended = true;
        CurrentlyExtendedMessage = messageViewModel;
    }
}
