using System.Collections.Concurrent;
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

public partial class EditorViewModel : ObservableObject, IRecipient<CurrentMessagesChanged>, IDisposable
{
    // Ick, but I can't be bothered making it better...
    static ConcurrentDictionary<string, UplinkMessageElementViewModel[]> _suspendedUplinkMessages = new();

    readonly AcarsConfiguration _configuration;
    readonly IMediator _mediator;
    readonly IErrorReporter _errorReporter;
    readonly IGuiInvoker _guiInvoker;
    readonly IWindowHandle _windowHandle;

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
            Message = "STANDBY DOWNLINK WITH VERY VERY VERY VERY VERY VERY VERY VERY VERY VERY VERY VERY VERY LONG MESSAGE",
            StandbySent = true
        }
    ];

    static AcarsConfiguration CreateTestConfiguration()
    {
        return new AcarsConfiguration
        {
            ServerEndpoint = "http://localhost:5000",
            Stations = ["TEST"],
            CurrentMessages = new CurrentMessagesConfiguration
            {
                MaxCurrentMessages = 50,
                HistoryTransferDelaySeconds = 10,
                PilotResponseTimeoutSeconds = 180,
                MaxDisplayMessageLength = 40,
                MaxExtendedMessageLength = 80
            },
            History = new HistoryConfiguration
            {
                MaxHistory = 100,
                MaxDisplayMessageLength = 40,
                MaxExtendedMessageLength = 80
            },
            ControllerLateSeconds = 120,
            PilotLateSeconds = 120,
            SpecialDownlinkMessages = ["STANDBY"],
            SpecialUplinkMessages = ["STANDBY", "REQUEST DEFERRED"],
            UplinkMessages = new UplinkMessagesConfiguration
            {
                MasterMessages =
                [
                    new UplinkMessageTemplate { Id = 147, Template = "REQUEST POSITION REPORT", Parameters = [], ResponseType = UplinkResponseType.NoResponse },
                    new UplinkMessageTemplate { Id = 123, Template = "SQUAWK [code]", Parameters = [new UplinkMessageParameter { Name = "code", Type = ParameterType.Code }], ResponseType = UplinkResponseType.WilcoUnable },
                    new UplinkMessageTemplate { Id = 20, Template = "CLIMB TO [lev]", Parameters = [new UplinkMessageParameter { Name = "lev", Type = ParameterType.Level }], ResponseType = UplinkResponseType.WilcoUnable },
                    new UplinkMessageTemplate { Id = 117, Template = "CONTACT [unit name] [freq]", Parameters = [new UplinkMessageParameter { Name = "unit name", Type = ParameterType.UnitName }, new UplinkMessageParameter { Name = "freq", Type = ParameterType.Frequency }], ResponseType = UplinkResponseType.WilcoUnable },
                    new UplinkMessageTemplate { Id = 169, Template = "[freetext]", Parameters = [new UplinkMessageParameter { Name = "freetext", Type = ParameterType.FreeText }], ResponseType = UplinkResponseType.Roger }
                ],
                PermanentMessages =
                [
                    new UplinkMessageReference { MessageId = 147 },
                    new UplinkMessageReference { MessageId = 123 },
                    new UplinkMessageReference { MessageId = 20 },
                    new UplinkMessageReference
                    {
                        MessageId = 117,
                        DefaultParameters = new Dictionary<string, string>
                        {
                            { "unit name", "MELBOURNE CTR" },
                            { "freq", "122.4" }
                        }
                    },
                    new UplinkMessageReference
                    {
                        MessageId = 169,
                        DefaultParameters = new Dictionary<string, string>
                        {
                            { "freetext", "REQUEST RECEIVED, RESPONSE WILL BE VIA VOICE" }
                        },
                        ResponseType = UplinkResponseType.Roger
                    },
                    new UplinkMessageReference
                    {
                        MessageId = 169,
                        DefaultParameters = new Dictionary<string, string>
                        {
                            { "freetext", "CRUISE CLIMB PROCEDURE NOT AVAILABLE IN AUSTRALIAN ADMINISTERED AIRSPACE" }
                        },
                        ResponseType = UplinkResponseType.Roger
                    }
                ],
                Groups =
                [
                    new UplinkMessageGroup
                    {
                        Name = "LEVEL",
                        Messages =
                        [
                            new UplinkMessageReference { MessageId = 20 }
                        ]
                    }
                ]
            }
        };
    }

    // For testing in the designer
    public EditorViewModel() : this("QFA1", _testDownlinkMessages, CreateTestConfiguration(), null!, null!, null!, null!)
    {
    }

#endif

    public EditorViewModel(
        string callsign,
        DownlinkMessageViewModel[] downlinkMessages,
        AcarsConfiguration configuration,
        IMediator mediator,
        IErrorReporter errorReporter,
        IGuiInvoker guiInvoker,
        IWindowHandle windowHandle)
    {
        _configuration = configuration;
        _mediator = mediator;
        _errorReporter = errorReporter;
        _guiInvoker = guiInvoker;
        _windowHandle = windowHandle;

        Callsign = callsign;
        DownlinkMessages = downlinkMessages;

        // Automatically select the last downlink message
        SelectedDownlinkMessage = downlinkMessages.LastOrDefault();

        MessageCategoryNames = _configuration.UplinkMessages.Groups
            .Select(g => g.Name)
            .ToArray();

        SelectedMessageCategory = null;
        DisplayMessageElements(_configuration.UplinkMessages.PermanentMessages);

        ClearUplinkMessage();

        WeakReferenceMessenger.Default.Register(this);
    }
    
    [ObservableProperty] private string callsign;

    [ObservableProperty] private DownlinkMessageViewModel[] downlinkMessages = [];
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(
        nameof(SendStandbyUplinkMessageCommand),
        nameof(DeferCommand),
        nameof(SendUnableDueTrafficUplinkMessageCommand),
        nameof(SendUnableDueAirspaceUplinkMessageCommand))]
    private DownlinkMessageViewModel? selectedDownlinkMessage;

    [ObservableProperty] private DownlinkMessageViewModel? currentlyExtendedDownlinkMessage;

    public bool ShowMessageCategories => !ShowHotButtons;
    [ObservableProperty] private string[] messageCategoryNames = [];

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowMessageCategories))]
    private string? selectedMessageCategory;

    [ObservableProperty] private UplinkMessageTemplateViewModel[] selectedMessageCategoryElements = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowMessageCategories))]
    [NotifyCanExecuteChangedFor(nameof(SuspendCommand))]
    private bool showHotButtons;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(
        nameof(EscapeCommand),
        nameof(RestoreCommand),
        nameof(SuspendCommand))]
    private UplinkMessageElementViewModel[] uplinkMessageElements = [];
    [ObservableProperty] private UplinkMessageElementViewModel? selectedUplinkMessageElement;

    [ObservableProperty] private string? error;

    partial void OnSelectedDownlinkMessageChanged(DownlinkMessageViewModel? _, DownlinkMessageViewModel? newValue)
    {
        // Show the hot buttons if a message has been selected
        ShowHotButtons = newValue is not null;
    }

    bool DownlinkIsSelected()
    {
        return SelectedDownlinkMessage is not null;
    }

    partial void OnShowHotButtonsChanged(bool value)
    {
        SelectedMessageCategory = value
            ? null
            : MessageCategoryNames.First();
    }

    [RelayCommand]
    void SelectMessageCategory(string? messageClass)
    {
        try
        {
            SelectedMessageCategory = messageClass;
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    partial void OnSelectedMessageCategoryChanged(string? value)
    {
        // If no category is selected, show permanent messages
        if (string.IsNullOrEmpty(value))
        {
            DisplayMessageElements(_configuration.UplinkMessages.PermanentMessages);
        }
        else
        {
            // Find the group by name
            var group = _configuration.UplinkMessages.Groups
                .FirstOrDefault(g => g.Name == value);

            if (group == null)
            {
                SelectedMessageCategoryElements = [];
                return;
            }

            DisplayMessageElements(group.Messages);
        }

        // Resolve each message reference to a template view model
    }

    void DisplayMessageElements(IEnumerable<UplinkMessageReference> messageReferences)
    {
        SelectedMessageCategoryElements = messageReferences
            .Select(ResolveMessageReference)
            .ToArray();
    }

    [RelayCommand(CanExecute = nameof(DownlinkIsSelected))]
    async Task SendStandbyUplinkMessage()
    {
        try
        {
            // Send the "STANDBY" uplink message
            await _mediator.Send(new SendStandbyUplinkRequest(SelectedDownlinkMessage!.OriginalMessage.Id, Callsign));
            SelectedDownlinkMessage = null;
            ClearUplinkMessage();
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
            await _mediator.Send(new SendDeferredUplinkRequest(SelectedDownlinkMessage!.OriginalMessage.Id, Callsign));
            SelectedDownlinkMessage = null;
            ClearUplinkMessage();
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

            // TODO: Do we need to do this? CurrentMessagesChanged should kick-in and remove it.
            var newDownlinkMessages = DownlinkMessages.Where(m => m != SelectedDownlinkMessage);
            DownlinkMessages = newDownlinkMessages.ToArray();
            SelectedDownlinkMessage = null;

            ClearUplinkMessage();
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

            // TODO: Do we need to do this? CurrentMessagesChanged should kick-in and remove it.
            var newDownlinkMessages = DownlinkMessages.Where(m => m != SelectedDownlinkMessage);
            DownlinkMessages = newDownlinkMessages.ToArray();
            SelectedDownlinkMessage = null;

            ClearUplinkMessage();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand]
    void AddMessageElement(UplinkMessageTemplateViewModel template)
    {
        try
        {
            var parts = ConvertToViewModel(template.MessageReference);

            // If a message element is selected, replace it with this one
            if (SelectedUplinkMessageElement is not null)
            {
                SelectedUplinkMessageElement.Replace(parts, template.ResponseType);
                UplinkMessageElements = UplinkMessageElements;
            }
            else if (UplinkMessageElements.Count () < 5)
            {
                // If no element is selected, append this to the list
                var firstBlankElement = UplinkMessageElements.FirstOrDefault(e => e.Parts.Length == 0);
                if (firstBlankElement is not null)
                {
                    firstBlankElement.Replace(parts, template.ResponseType);

                    // Trigger property change
                    // TODO: Find a better way to do this
                    UplinkMessageElements = UplinkMessageElements;
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

            UplinkMessageElements = elements.ToArray();
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
                SelectedUplinkMessageElement = null;
            }

            // Do nothing if this is the last element, and it's already blank
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    [RelayCommand(CanExecute = nameof(CanEscape))]
    void Escape()
    {
        ClearUplinkMessage();
        SelectedMessageCategory = null;
    }

    bool CanEscape() => UplinkMessageElements.Any();

    [RelayCommand(CanExecute = nameof(CanRestore))]
    void Restore()
    {
        ClearUplinkMessage();

        if (_suspendedUplinkMessages.TryRemove(Callsign, out var suspendedUplinkMessageElements))
        {
            UplinkMessageElements = suspendedUplinkMessageElements;
            SelectedUplinkMessageElement = null;
        }
    }

    bool CanRestore()
    {
        return _suspendedUplinkMessages.ContainsKey(Callsign);
    }

    [RelayCommand(CanExecute = nameof(CanSuspend))]
    void Suspend()
    {
        _suspendedUplinkMessages[callsign] = UplinkMessageElements.ToArray();
        ClearUplinkMessage();

        // Select the most recent downlink message if none is already selected
        if (SelectedDownlinkMessage is null)
            SelectedDownlinkMessage = DownlinkMessages.LastOrDefault();

        SelectedMessageCategory = null;
    }

    bool CanSuspend()
    {
        // Cannot suspend replies to downlinks
        if (SelectedDownlinkMessage is not null)
            return false;
        
        // Cannot suspend in Mode 2
        if (ShowHotButtons)
            return false;

        // Cannot suspend empty messages
        if (UplinkMessageElements.All(m => m.IsEmpty))
            return false;

        // Cannot suspend when there is already a suspended message
        if (_suspendedUplinkMessages.ContainsKey(Callsign))
            return false;

        return true;
    }

    [RelayCommand]
    async Task SendUplinkMessage()
    {
        try
        {
            var (uplinkMessageContent, uplinkMessageResponseType) = ConstructUplinkMessage();
            
            // Remove the selected downlink message and select the most recent one
            var downlinkMessage = SelectedDownlinkMessage;
            if (SelectedDownlinkMessage is not null)
            {
                var newDownlinkMessages = new List<DownlinkMessageViewModel>();
                newDownlinkMessages.AddRange(DownlinkMessages.Where(d => d != SelectedDownlinkMessage));
                SelectedDownlinkMessage = newDownlinkMessages.LastOrDefault();
            }
            
            await _mediator.Send(new SendUplinkRequest(
                Callsign,
                downlinkMessage?.OriginalMessage.Id,
                uplinkMessageResponseType,
                uplinkMessageContent));
        
            ClearUplinkMessage();
        
            if (SelectedDownlinkMessage is not null)
                return;
            
            // Close the window if there are no more downlink messages remaining
            _windowHandle.Close();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    void ClearUplinkMessage()
    {
        UplinkMessageElements = [new UplinkMessageElementViewModel()];
        SelectedUplinkMessageElement = null;
    }

    IUplinkMessageElementComponentViewModel[] ConvertToViewModel(UplinkMessageReference reference)
    {
        // Get the master message template
        var masterMessage = _configuration.UplinkMessages.MasterMessages
            .FirstOrDefault(m => m.Id == reference.MessageId);

        if (masterMessage == null)
            throw new InvalidOperationException($"Master message with ID {reference.MessageId} not found");

        var template = masterMessage.Template;
        var parts = new List<IUplinkMessageElementComponentViewModel>();
        var currentText = new StringBuilder();
        var insideBrackets = false;
        var parameterName = new StringBuilder();

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
                        parts.Add(new UplinkMessageTextElementComponentViewModel(currentText.ToString()));
                        currentText.Clear();
                    }
                    insideBrackets = true;
                    parameterName.Clear();
                    break;
                }

                case ']' when insideBrackets:
                {
                    // Transition from inside to outside brackets
                    var paramName = parameterName.ToString();
                    var templateElement = new UplinkMessageTemplateElementComponentViewModel($"[{paramName}]");

                    // Check if there's a default value for this parameter
                    if (reference.DefaultParameters?.TryGetValue(paramName, out var defaultValue) == true)
                    {
                        // Pre-fill the template element with the default value
                        templateElement.Value = defaultValue;
                    }

                    parts.Add(templateElement);
                    insideBrackets = false;
                    break;
                }

                default:
                    if (insideBrackets)
                    {
                        parameterName.Append(c);
                    }
                    else
                    {
                        currentText.Append(c);
                    }
                    break;
            }
        }

        // Handle any remaining text
        if (currentText.Length > 0)
        {
            if (insideBrackets)
            {
                // Unclosed bracket - treat as template part with what we have
                var templateElement = new UplinkMessageTemplateElementComponentViewModel($"[{parameterName}]");
                parts.Add(templateElement);
            }
            else
            {
                // Normal text
                parts.Add(new UplinkMessageTextElementComponentViewModel(currentText.ToString()));
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
                if (uplinkMessageElementPart is UplinkMessageTextElementComponentViewModel textPart)
                {
                    content += textPart.Value;
                    continue;
                }

                if (uplinkMessageElementPart is UplinkMessageTemplateElementComponentViewModel templatePart)
                {
                    if (string.IsNullOrEmpty(templatePart.Value))
                        throw new Exception("Uplink message is invalid");
                    
                    content += $"@{templatePart.Value}@";
                }

                // TODO: Error?
            }
            
            var currentResponseRank = _responseTypeRank[responseType];
            var newResponseRank = _responseTypeRank[_responseTypeMap[uplinkMessageElement.ResponseType]];
            if (newResponseRank > currentResponseRank)
                responseType = _responseTypeMap[uplinkMessageElement.ResponseType];
        }

        return (content.Trim(), responseType);
    }

    public void Receive(CurrentMessagesChanged message)
    {
        _guiInvoker.InvokeOnGUI(async _ => await LoadDownlinkMessagesAsync());
    }

    async Task LoadDownlinkMessagesAsync()
    {
        try
        {
            var response = await _mediator.Send(new GetCurrentDialoguesRequest());

            var downlinkViewModels = new List<DownlinkMessageViewModel>();
            
            foreach (var dialogue in response.Dialogues)
            {
                if (dialogue.Callsign != Callsign)
                    continue;
                
                foreach (var message in dialogue.Messages)
                {
                    if (message is not DownlinkMessage downlinkMessage || downlinkMessage.IsClosed || downlinkMessage.ResponseType == CpdlcDownlinkResponseType.NoResponse)
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

    UplinkMessageTemplateViewModel ResolveMessageReference(UplinkMessageReference reference)
    {
        var masterMessage = _configuration.UplinkMessages.MasterMessages
            .FirstOrDefault(m => m.Id == reference.MessageId);

        if (masterMessage == null)
            throw new InvalidOperationException($"Master message with ID {reference.MessageId} not found");

        var template = masterMessage.Template;

        // Replace template parameters with default values for display purposes
        if (reference.DefaultParameters != null)
        {
            foreach (var kvp in reference.DefaultParameters)
            {
                var paramName = kvp.Key;
                var paramValue = kvp.Value;

                template = template.Replace($"[{paramName}]", paramValue);
            }
        }

        // Use the reference's response type if specified, otherwise use the master message's response type
        var responseType = reference.ResponseType ?? masterMessage.ResponseType;

        var isFreeText = masterMessage.Id == 169;
        var isRevision = masterMessage.Id == 170;

        var viewModel = new UplinkMessageTemplateViewModel(
            template,
            responseType,
            isFreeText,
            isRevision,
            reference);

        return viewModel;
    }

    private readonly IDictionary<UplinkResponseType, CpdlcUplinkResponseType> _responseTypeMap = new Dictionary<UplinkResponseType, CpdlcUplinkResponseType>
    {
        { UplinkResponseType.WilcoUnable, CpdlcUplinkResponseType.WilcoUnable },
        { UplinkResponseType.AffirmativeNegative, CpdlcUplinkResponseType.AffirmativeNegative },
        { UplinkResponseType.Roger, CpdlcUplinkResponseType.Roger },
        { UplinkResponseType.NoResponse, CpdlcUplinkResponseType.NoResponse },
    };

    private readonly IDictionary<CpdlcUplinkResponseType, int> _responseTypeRank = new Dictionary<CpdlcUplinkResponseType, int>
    {
        { CpdlcUplinkResponseType.WilcoUnable, 3 },
        { CpdlcUplinkResponseType.AffirmativeNegative, 2 },
        { CpdlcUplinkResponseType.Roger, 1 },
        { CpdlcUplinkResponseType.NoResponse, 0 },
    };
}
