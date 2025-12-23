using System.Collections.ObjectModel;
using ACARSPlugin.Configuration;
using ACARSPlugin.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;

namespace ACARSPlugin.ViewModels;

public partial class HistoryViewModel : ObservableObject,
    IRecipient<HistoryMessagesChanged>,
    IDisposable
{
    private readonly AcarsConfiguration _configuration;
    private readonly IMediator _mediator;
    private readonly IGuiInvoker _guiInvoker;
    private readonly IErrorReporter _errorReporter;
    private bool _disposed;

    public HistoryViewModel(
        AcarsConfiguration configuration,
        IMediator mediator,
        IGuiInvoker guiInvoker,
        IErrorReporter errorReporter,
        string? initialCallsign = null)
    {
        _configuration = configuration;
        _mediator = mediator;
        _guiInvoker = guiInvoker;
        _errorReporter = errorReporter;

        _callsign = initialCallsign ?? string.Empty;

        WeakReferenceMessenger.Default.Register(this);

        // Initial load if we have a callsign
        if (!string.IsNullOrWhiteSpace(initialCallsign))
        {
            _ = LoadDialoguesAsync();
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CallsignButtonText))]
    private string? _callsign;

    public string CallsignButtonText => string.IsNullOrWhiteSpace(Callsign) ? "ACID" : Callsign;

    [ObservableProperty]
    private DialogueHistoryViewModel[] _dialogues = [];

    [ObservableProperty]
    private HistoryMessageViewModel? _currentlyExtendedMessage;

    private async Task LoadDialoguesAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Callsign))
            {
                Dialogues = [];
                return;
            }

            var response = await _mediator.Send(new GetHistoryDialoguesRequest(Callsign));

            var dialogueViewModels = response.Dialogues
                .Select(d => new DialogueHistoryViewModel
                {
                    Messages = new ObservableCollection<HistoryMessageViewModel>(
                        d.Messages.Select(m => new HistoryMessageViewModel(m, _configuration.History))),
                    FirstMessageTime = d.Messages.OrderBy(m => m.Time).First().Time
                })
                .ToArray();

            Dialogues = dialogueViewModels;
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    private async Task LoadMessages()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Callsign))
            {
                Callsign = null;
                Dialogues = [];
                return;
            }

            Callsign = Callsign.Trim().ToUpperInvariant();
            await LoadDialoguesAsync();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    public void Receive(HistoryMessagesChanged message)
    {
        if (_disposed)
            return;

        _guiInvoker.InvokeOnGUI(async () =>
        {
            if (_disposed)
                return;

            try
            {
                if (!string.IsNullOrWhiteSpace(Callsign))
                    await LoadDialoguesAsync();
            }
            catch (Exception ex)
            {
                _errorReporter.ReportError(ex);
            }
        });
    }

    [RelayCommand]
    private void ToggleExtendedDisplay(HistoryMessageViewModel messageViewModel)
    {
        try
        {
            if (CurrentlyExtendedMessage == messageViewModel)
            {
                messageViewModel.IsExtended = false;
                CurrentlyExtendedMessage = null;
                return;
            }

            if (CurrentlyExtendedMessage != null)
                CurrentlyExtendedMessage.IsExtended = false;

            messageViewModel.IsExtended = true;
            CurrentlyExtendedMessage = messageViewModel;
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        WeakReferenceMessenger.Default.Unregister<HistoryMessagesChanged>(this);
        _disposed = true;
    }
}