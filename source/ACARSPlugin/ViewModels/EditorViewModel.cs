using System.Text;
using ACARSPlugin.Configuration;
using ACARSPlugin.Messages;
using ACARSPlugin.Model;
using ACARSPlugin.Server.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;

namespace ACARSPlugin.ViewModels;

// TODO: Associate downlink messages with uplink classes.
// TODO: ESCAPE function
// TODO: RESTORE function
// TODO: SUSPEND function

public partial class EditorViewModel : ObservableObject, IRecipient<CurrentMessagesChanged>, IDisposable
{
    // TODO: Source these from configuration
    private readonly string _permanentMessageClassName = Testing.PermanentMessageClassName;
    readonly UplinkMessageTemplates _uplinkMessageTemplates = Testing.UplinkMessageTemplates;

    readonly IMediator _mediator;
    readonly IErrorReporter _errorReporter;
    readonly IGuiInvoker _guiInvoker;

#if DEBUG
    
    static DownlinkMessageViewModel[] _testDownlinkMessages = 
    [
        new()
        {
            Received = DateTimeOffset.Now,
            Message = "DEFERRED DOWNLINK",
            Deferred = true
        },

        new()
        {
            Received = DateTimeOffset.Now,
            Message = "STANDBY DOWNLINK",
            StandbySent = true
        }
    ];
    
    // For testing in the designer
    public EditorViewModel() : this("QFA1", _testDownlinkMessages, null!, null!, null!)
    {
    }
    
#endif

    public EditorViewModel(string callsign, DownlinkMessageViewModel[] downlinkMessages, IMediator mediator, IErrorReporter errorReporter, IGuiInvoker guiInvoker)
    {
        _mediator = mediator;
        _errorReporter = errorReporter;
        _guiInvoker = guiInvoker;

        Callsign = callsign;
        DownlinkMessages = downlinkMessages;

        if (downlinkMessages.Any())
        {
            // Automatically select the last downlink message
            downlinkMessages.Last().Selected = true;
            SelectedDownlinkMessage =  downlinkMessages.Last();

            // TODO: Proper Mode 2 setup
            ShowHotButtons = true;
        }
        else
        {
            ShowHotButtons = false;
        }

        // Don't show the permanent class
        MessageClasses = _uplinkMessageTemplates.Messages.Keys
            .Where(s => s != _permanentMessageClassName)
            .ToList();

        // Select the permanent class by default
        SelectMessageClass(Testing.PermanentMessageClassName);

        // Start with a blank line
        ClearConstructionArea();

        // Register for message updates
        WeakReferenceMessenger.Default.Register(this);
    }
    
    [ObservableProperty]
    private string callsign = "Unknown";

    [ObservableProperty] private DownlinkMessageViewModel[] downlinkMessages = [];

    [ObservableProperty]
    private DownlinkMessageViewModel? selectedDownlinkMessage;

    partial void OnSelectedDownlinkMessageChanged(DownlinkMessageViewModel? oldValue, DownlinkMessageViewModel? newValue)
    {
        // Clear the Selected property on the previously selected message
        if (oldValue is not null)
        {
            oldValue.Selected = false;
        }

        // Set the Selected property on the newly selected message
        if (newValue is not null)
        {
            newValue.Selected = true;
        }

        ShowHotButtons = newValue is not null;
    }

    public bool ShowMessageClassButtons => !ShowHotButtons;

    // TODO: Populate with relevant messages in Mode 2
    [ObservableProperty]
    private List<string> messageClasses = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowMessageClassButtons))]
    private string? selectedMessageClass;

    [ObservableProperty]
    private UplinkMessageTemplate[] selectedMessageClassElements = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowMessageClassButtons))]
    private bool showHotButtons;

    partial void OnShowHotButtonsChanged(bool value)
    {
        if (value)
        {
            SelectedMessageClass = Testing.PermanentMessageClassName;
        }
        else
        {
            SelectedMessageClass = MessageClasses.First();
        }
    }

    [ObservableProperty]
    private IEnumerable<UplinkMessageElementViewModel> uplinkMessageElements = [];

    [ObservableProperty] private UplinkMessageElementViewModel? selectedUplinkMessageElement;

    [ObservableProperty]
    private string? error;

    [RelayCommand]
    void SelectMessageClass(string? messageClass)
    {
        try
        {
            SelectedMessageClass = messageClass;
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    partial void OnSelectedMessageClassChanged(string? value)
    {
        SelectedMessageClassElements = string.IsNullOrEmpty(value)
            ? []
            : _uplinkMessageTemplates.Messages[value]; 
    }

    bool DownlinkIsSelected()
    {
        return SelectedDownlinkMessage is not null;
    }

    [RelayCommand(CanExecute = nameof(DownlinkIsSelected))]
    async Task SendStandbyUplinkMessage()
    {
        try
        {
            // Send the "STANDBY" uplink message
            await _mediator.Send(new SendStandbyUplinkRequest(SelectedDownlinkMessage.OriginalMessage.Id, Callsign));

            // De-select the downlink message (transition to Mode 1)
            SelectedDownlinkMessage = null;

            ClearConstructionArea();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand(CanExecute = nameof(DownlinkIsSelected))]
    async Task Defer()
    {
        try
        {
            // Send the "REQUEST DEFERRED" uplink message
            await _mediator.Send(new SendDeferredUplinkRequest(SelectedDownlinkMessage.OriginalMessage.Id, Callsign));

            // De-select the downlink message (transition to Mode 1)
            SelectedDownlinkMessage = null;

            ClearConstructionArea();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    void Edit()
    {
        try
        {
            ShowHotButtons = false;
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }
    
    [RelayCommand(CanExecute = nameof(DownlinkIsSelected))]
    async Task SendUnableDueTrafficUplinkMessage()
    {
        try
        {
            // Send the "UNABLE" and "DUE TO TRAFFIC" uplink messages
            await _mediator.Send(new SendUnableUplinkRequest(SelectedDownlinkMessage.OriginalMessage.Id, Callsign, "DUE TO TRAFFIC."));

            // TODO: Downlink message removed, and moved to History

            // De-select the downlink message (transition to Mode 1)
            SelectedDownlinkMessage = null;

            ClearConstructionArea();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }
    
    [RelayCommand(CanExecute = nameof(DownlinkIsSelected))]
    async Task SendUnableDueAirspaceUplinkMessage()
    {
        try
        {
            // Send the "UNABLE" and "DUE TO AIRSPACE RESTRICTION" uplink messages
            await _mediator.Send(new SendUnableUplinkRequest(SelectedDownlinkMessage.OriginalMessage.Id, Callsign, "DUE TO AIRSPACE RESTRICTION."));

            // TODO: Downlink message removed, and moved to History

            // De-select the downlink message (transition to Mode 1)
            SelectedDownlinkMessage = null;

            ClearConstructionArea();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    void AddMessageElement(UplinkMessageTemplate template)
    {
        try
        {
            var parts = ConvertToParts(template.Template);

            // TODO: If a message element is selected, replace it with this one
            if (SelectedUplinkMessageElement is not null)
            {
                SelectedUplinkMessageElement.Replace(parts, template.ResponseType);
            }
            else if (UplinkMessageElements.Count () < 5)
            {
                var firstBlankElement = UplinkMessageElements.FirstOrDefault(e => e.Parts.Length == 0);
                if (firstBlankElement is not null)
                {
                    firstBlankElement.Replace(parts, template.ResponseType);
                }
                else
                {
                    var newMessageElements = UplinkMessageElements.ToList();
                    newMessageElements.Add(new UplinkMessageElementViewModel(parts, template.ResponseType));

                    UplinkMessageElements = newMessageElements.ToArray();
                }
            }

            // TODO: Exceeded 5 elements, show an error
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }
    
    [RelayCommand]
    void ToggleMessageElementSelection(UplinkMessageElementViewModel element)
    {
        try
        {
            if (SelectedUplinkMessageElement == element)
            {
                SelectedUplinkMessageElement = null;
            }
            else
            {
                SelectedUplinkMessageElement = element;
            }
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    void InsertMessageElementAbove(UplinkMessageElementViewModel element)
    {
        try
        {
            // Don't exceed 5 elements
            if (UplinkMessageElements.Count() >= 5)
                return;

            var elements = UplinkMessageElements.ToList();
            var index = elements.IndexOf(element);

            if (index < 0)
                return;

            // Insert a new blank element above the clicked one
            var newElement = new UplinkMessageElementViewModel();
            elements.Insert(index, newElement);

            UplinkMessageElements = elements;
            SelectedUplinkMessageElement = newElement;
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    void ClearMessageElement(UplinkMessageElementViewModel element)
    {
        try
        {
            if (element.Parts.Any())
            {
                // If this element is not blank, clear it
                element.Clear();
            }
            else if (UplinkMessageElements.Count() > 1)
            {
                // If this element is blank and there's more than one element, remove it
                var newMessages = UplinkMessageElements.ToList();
                newMessages.Remove(element);

                UplinkMessageElements = newMessages.ToArray();
            }

            // Do nothing if this is the last element, and it's already blank
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    async Task SendUplinkMessage()
    {
        try
        {
            var (uplinkMessageContent, uplinkMessageResponseType) = ConstructUplinkMessage();

            await _mediator.Send(new SendUplinkRequest(
                Callsign,
                SelectedDownlinkMessage?.OriginalMessage.Id,
                uplinkMessageResponseType,
                uplinkMessageContent));
        
            ClearConstructionArea();
        
            // TODO: If any open downlinks have been received since the window was opened, select the next one.
            
            // TODO: Close the window.
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    void ClearConstructionArea()
    {
        UplinkMessageElements = [new UplinkMessageElementViewModel()];
        SelectedUplinkMessageElement = null;
    }

    IUplinkMessagePartViewModel[] ConvertToParts(string template)
    {
        var parts = new List<IUplinkMessagePartViewModel>();
        var currentText = new StringBuilder();
        var insideBrackets = false;

        foreach (var c in template)
        {
            switch (c)
            {
                case '[' when !insideBrackets:
                {
                    // Transition from outside to inside brackets
                    if (currentText.Length > 0)
                    {
                        // Save the text part
                        parts.Add(new UplinkMessageTextPartViewModel { Value = currentText.ToString() });
                        currentText.Clear();
                    }
                    insideBrackets = true;
                    currentText.Append(c); // Include the opening bracket
                    break;
                }

                case ']' when insideBrackets:
                    // Transition from inside to outside brackets
                    currentText.Append(c); // Include the closing bracket
                    parts.Add(new UplinkMessageTemplatePartViewModel { Placeholder = currentText.ToString() });
                    currentText.Clear();
                    insideBrackets = false;
                    break;

                default:
                    currentText.Append(c);
                    break;
            }
        }

        // Handle any remaining text
        if (currentText.Length > 0)
        {
            if (insideBrackets)
            {
                // Unclosed bracket - treat as template part with what we have
                parts.Add(new UplinkMessageTemplatePartViewModel { Placeholder = currentText.ToString() });
            }
            else
            {
                // Normal text
                parts.Add(new UplinkMessageTextPartViewModel { Value = currentText.ToString() });
            }
        }

        return parts.ToArray();
    }

    (string, CpdlcUplinkResponseType) ConstructUplinkMessage()
    {
        var content = string.Empty;
        var responseType = CpdlcUplinkResponseType.NoResponse;
        
        foreach (var uplinkMessageElement in UplinkMessageElements)
        {
            if (!string.IsNullOrEmpty(content))
            {
                content += ". ";
            }
            
            foreach (var uplinkMessageElementPart in uplinkMessageElement.Parts)
            {
                if (uplinkMessageElementPart is UplinkMessageTextPartViewModel textPart)
                {
                    content += textPart.Value;
                    continue;
                }

                if (uplinkMessageElementPart is UplinkMessageTemplatePartViewModel templatePart)
                {
                    if (string.IsNullOrEmpty(templatePart.Value))
                        throw new Exception("Uplink message is invalid");
                    
                    content += $"@{templatePart.Value}@";
                }

                // TODO: Error?
            }
            
            // TODO: Find a better way to determine the response type.
            //  What do the official docs say?
            var currentResponseRank = responseTypeRank[responseType];
            var newResponseRank = responseTypeRank[responseTypeMap[uplinkMessageElement.ResponseType]];
            if (newResponseRank > currentResponseRank)
                responseType = responseTypeMap[uplinkMessageElement.ResponseType];
        }

        return (content.Trim(), responseType);
    }

    public void Receive(CurrentMessagesChanged message)
    {
        _guiInvoker.InvokeOnGUI(async () => await LoadDownlinkMessagesAsync());
    }

    async Task LoadDownlinkMessagesAsync()
    {
        try
        {
            var response = await _mediator.Send(new GetCurrentDialoguesRequest());

            var downlinkViewModels = new List<DownlinkMessageViewModel>();
            
            foreach (var dialogue in response.Dialogues)
            {
                foreach (var message in dialogue.Messages)
                {
                    if (message is not DownlinkMessage downlinkMessage || downlinkMessage.IsClosed)
                        continue;
                    
                    var downlinkMessageViewModel = new DownlinkMessageViewModel(
                        downlinkMessage,
                        standbySent: dialogue.HasStandbyResponse(downlinkMessage.Id),
                        deferred: dialogue.HasDeferredResponse(downlinkMessage.Id));
                
                    downlinkViewModels.Add(downlinkMessageViewModel);
                }
            }

            DownlinkMessages = downlinkViewModels.ToArray();

            // Try to maintain the current selection if the message still exists
            if (SelectedDownlinkMessage is not null)
            {
                var stillExists = downlinkViewModels.Any(vm => vm.OriginalMessage.Id == SelectedDownlinkMessage.OriginalMessage?.Id);
                SelectedDownlinkMessage = stillExists
                    ? downlinkViewModels.First(vm => vm.OriginalMessage.Id == SelectedDownlinkMessage.OriginalMessage.Id)
                    : null;
            }
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<CurrentMessagesChanged>(this);
    }

    private readonly IDictionary<UplinkResponseType, CpdlcUplinkResponseType> responseTypeMap = new Dictionary<UplinkResponseType, CpdlcUplinkResponseType>
    {
        { UplinkResponseType.WilcoUnable, CpdlcUplinkResponseType.WilcoUnable },
        { UplinkResponseType.AffirmativeNegative, CpdlcUplinkResponseType.AffirmativeNegative },
        { UplinkResponseType.Roger, CpdlcUplinkResponseType.Roger },
        { UplinkResponseType.NoResponse, CpdlcUplinkResponseType.NoResponse },
    };

    private readonly IDictionary<CpdlcUplinkResponseType, int> responseTypeRank = new Dictionary<CpdlcUplinkResponseType, int>
    {
        { CpdlcUplinkResponseType.WilcoUnable, 3 },
        { CpdlcUplinkResponseType.AffirmativeNegative, 2 },
        { CpdlcUplinkResponseType.Roger, 1 },
        { CpdlcUplinkResponseType.NoResponse, 0 },
    };
}

public partial class UplinkMessageElementViewModel : ObservableObject
{
    public UplinkMessageElementViewModel()
    {
    }

    public UplinkMessageElementViewModel(
        IUplinkMessagePartViewModel[] parts,
        UplinkResponseType responseType)
    {
        Parts = parts;
        ResponseType = responseType;
    }

    [ObservableProperty]
    private IUplinkMessagePartViewModel[] parts = [];
    
    [ObservableProperty]
    UplinkResponseType responseType = UplinkResponseType.NoResponse;

    public void Replace(IUplinkMessagePartViewModel[] parts, UplinkResponseType responseType)
    {
        Parts = parts;
        ResponseType = responseType;
    }

    public void Clear()
    {
        Parts = [];
        ResponseType =  UplinkResponseType.NoResponse;
    }
}

public interface IUplinkMessagePartViewModel;

public partial class UplinkMessageTextPartViewModel : ObservableObject, IUplinkMessagePartViewModel
{
    [ObservableProperty]
    private string value = string.Empty;
}

public partial class UplinkMessageTemplatePartViewModel : ObservableObject, IUplinkMessagePartViewModel
{
    [ObservableProperty]
    private string placeholder = string.Empty;

    [ObservableProperty]
    private string? value;

    [ObservableProperty]
    private bool isEditing;
}
