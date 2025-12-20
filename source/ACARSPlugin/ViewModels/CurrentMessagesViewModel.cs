using System.Collections.ObjectModel;
using ACARSPlugin.Configuration;
using ACARSPlugin.Messages;
using ACARSPlugin.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;

namespace ACARSPlugin.ViewModels;

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
    // Test constructor for design-time data
    public CurrentMessagesViewModel()
    {
        var currentMessagesConfiguration = new CurrentMessagesConfiguration();

        // Create test dialogues with sample messages
        var testGroups = new ObservableCollection<DialogueViewModel>();

        // First dialogue group - QFA123
        var qfa123Group = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-10),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        // Add some test messages for QFA123
        var uplinkMsg1 = new Model.UplinkMessage(
            1, "QFA123", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
            "UPLINK NORMAL STATE NOT ACKNOWLEDGED", DateTimeOffset.UtcNow.AddMinutes(-10));

        var downlinkMsg1 = new Model.DownlinkMessage(
            2, "QFA123", Server.Contracts.CpdlcDownlinkResponseType.NoResponse,
            "DOWNLINK NORMAL STATE NOT ACKNOWLEDGED", DateTimeOffset.UtcNow.AddMinutes(-9), 1);

        qfa123Group.Messages.Add(new MessageViewModel(uplinkMsg1, currentMessagesConfiguration));
        qfa123Group.Messages.Add(new MessageViewModel(downlinkMsg1, currentMessagesConfiguration));

        testGroups.Add(qfa123Group);

        // Second dialogue group - UAL456
        var ual456Group = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-5),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        // Add test messages for UAL456 with a pilot late response
        var uplinkMsg2 = new Model.UplinkMessage(
            3, "UAL456", Server.Contracts.CpdlcUplinkResponseType.Roger,
            "UPLINK PILOT ANSWER LATE NOT ACKNOWLEDGED", DateTimeOffset.UtcNow.AddMinutes(-5))
        { IsPilotLate = true };

        var downlinkMsg2 = new Model.DownlinkMessage(
            4, "UAL456", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
            "DOWNLINK NORMAL STATE NOT ACKNOWLEDGED", DateTimeOffset.UtcNow.AddMinutes(-3));

        ual456Group.Messages.Add(new MessageViewModel(uplinkMsg2, currentMessagesConfiguration));
        ual456Group.Messages.Add(new MessageViewModel(downlinkMsg2, currentMessagesConfiguration));

        testGroups.Add(ual456Group);

        // Third dialogue group - DAL789 with overflow message
        var dal789Group = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-2),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        var downlinkMsg3 = new Model.DownlinkMessage(
            5, "DAL789", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
            "DOWNLINK OVERFLOW MESSAGE SHOWING ASTERISK PREFIX NOT ACKNOWLEDGED VERY LONG TEXT",
            DateTimeOffset.UtcNow.AddMinutes(-2));

        dal789Group.Messages.Add(new MessageViewModel(downlinkMsg3, currentMessagesConfiguration));

        testGroups.Add(dal789Group);

        // Fourth dialogue group - AAL123 with WaitingForResponse state
        var aal123Group = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-8),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        var uplinkMsg3 = new Model.UplinkMessage(
            6, "AAL123", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
            "UPLINK WAITING FOR RESPONSE NOT ACKNOWLEDGED", DateTimeOffset.UtcNow.AddMinutes(-8));

        aal123Group.Messages.Add(new MessageViewModel(uplinkMsg3, currentMessagesConfiguration));

        testGroups.Add(aal123Group);

        // Fifth dialogue group - SWA456 with TransmissionFailure state
        var swa456Group = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-6),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        var uplinkMsg4 = new Model.UplinkMessage(
            7, "SWA456", Server.Contracts.CpdlcUplinkResponseType.WilcoUnable,
            "UPLINK TRANSMISSION FAILURE NOT ACKNOWLEDGED", DateTimeOffset.UtcNow.AddMinutes(-6))
        { IsTransmissionFailed = true };

        swa456Group.Messages.Add(new MessageViewModel(uplinkMsg4, currentMessagesConfiguration));

        testGroups.Add(swa456Group);

        // Sixth dialogue group - JBU789 with Acknowledged messages
        var jbu789Group = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-12),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        var downlinkMsg4 = new Model.DownlinkMessage(
            9, "JBU789", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
            "DOWNLINK NORMAL STATE ACKNOWLEDGED", DateTimeOffset.UtcNow.AddMinutes(-11))
        {
            IsAcknowledged = true
        };

        jbu789Group.Messages.Add(new MessageViewModel(downlinkMsg4, currentMessagesConfiguration));

        testGroups.Add(jbu789Group);

        // Seventh dialogue group - VIR101 with Closed messages
        var vir101Group = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-15),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        var uplinkMsg6 = new Model.UplinkMessage(
            10, "VIR101", Server.Contracts.CpdlcUplinkResponseType.Roger,
            "UPLINK CLOSED STATE NOT ACKNOWLEDGED", DateTimeOffset.UtcNow.AddMinutes(-15));

        var downlinkMsg5 = new Model.DownlinkMessage(
            11, "VIR101", Server.Contracts.CpdlcDownlinkResponseType.NoResponse,
            "DOWNLINK CLOSED STATE NOT ACKNOWLEDGED", DateTimeOffset.UtcNow.AddMinutes(-14), 10);

        vir101Group.Messages.Add(new MessageViewModel(uplinkMsg6, currentMessagesConfiguration));
        vir101Group.Messages.Add(new MessageViewModel(downlinkMsg5, currentMessagesConfiguration));

        testGroups.Add(vir101Group);

        // Eighth dialogue group - EZY202 with acknowledged Closed messages
        var ezy202Group = new DialogueViewModel
        {
            FirstMessageTime = DateTimeOffset.UtcNow.AddMinutes(-1),
            Messages = new ObservableCollection<MessageViewModel>()
        };

        var downlinkMsg6 = new Model.DownlinkMessage(
            13, "EZY202", Server.Contracts.CpdlcDownlinkResponseType.ResponseRequired,
            "DOWNLINK NORMAL CLOSED ACKNOWLEDGED", DateTimeOffset.UtcNow.AddMinutes(-1))
        {
            IsAcknowledged = true
        };

        ezy202Group.Messages.Add(new MessageViewModel(downlinkMsg6, currentMessagesConfiguration));

        testGroups.Add(ezy202Group);

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
