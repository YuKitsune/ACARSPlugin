using System.Text;
using ACARSPlugin.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ACARSPlugin.ViewModels;

// TODO: Associate downlink messages with uplink classes.

public partial class EditorViewModel : ObservableObject
{
    // TODO: Source these from configuration
    private readonly string _permanentMessageClassName = Testing.PermanentMessageClassName;
    readonly UplinkMessageTemplates _uplinkMessageTemplates = Testing.UplinkMessageTemplates;

#if DEBUG
    
    static DownlinkMessageViewModel[] _testDownlinkMessages = 
    [
        new()
        {
            Received = DateTimeOffset.Now,
            Message = "STINKY POO POO",
            Deferred = true
        },

        new()
        {
            Received = DateTimeOffset.Now,
            Message = "REQUEST CLIMB UP YOUR ASS"
        }
    ];
    
    // For testing in the designer
    public EditorViewModel() : this("QFA1", _testDownlinkMessages)
    {
    }
    
#endif

    public EditorViewModel(string callsign, DownlinkMessageViewModel[] downlinkMessages)
    {
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

    // TODO: Populate with relevent messages in Mode 2
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
        SelectedMessageClass = messageClass;
    }

    partial void OnSelectedMessageClassChanged(string? value)
    {
        SelectedMessageClassElements = string.IsNullOrEmpty(value)
            ? []
            : _uplinkMessageTemplates.Messages[value]; 
    }

    [RelayCommand]
    void SendStandbyUplinkMessage()
    {
        // TODO: Send the "STANDBY" uplink message
        // TODO: De-select the downlink message
        // TODO: Transition to Mode1
        // TODO: Show Permanent message elements
        ClearConstructionArea();
        throw new NotImplementedException();
    }

    [RelayCommand]
    void Defer()
    {
        // TODO: Send the "REQUEST DEFERRED" uplink message
        // TODO: De-select the downlink message
        // TODO: Transition to Mode1
        // TODO: Show Permanent message elements
        ClearConstructionArea();
        throw new NotImplementedException();
    }

    [RelayCommand]
    void Edit()
    {
        ShowHotButtons = false;
    }
    
    [RelayCommand]
    void SendUnableDueTrafficUplinkMessage()
    {
        // TODO: Send the "UNABLE" and "DUE TO TRAFFIC" uplink messages
        // TODO: Downlink message removed, and moved to History
        // TODO: Transition to Mode1
        // TODO: Show Permanent message elements
        ClearConstructionArea();
        throw new NotImplementedException();
    }
    
    [RelayCommand]
    void SendUnableDueAirspaceUplinkMessage()
    {
        // TODO: Send the "UNABLE" and "DUE TO AIRSPACE RESTRICTION" uplink messages
        // TODO: Downlink message removed, and moved to History
        // TODO: Transition to Mode1
        // TODO: Show Permanent message elements
        ClearConstructionArea();
        throw new NotImplementedException();
    }

    [RelayCommand]
    void AddMessageElement(UplinkMessageTemplate template)
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
    
    [RelayCommand]
    void ToggleMessageElementSelection(UplinkMessageElementViewModel element)
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

    [RelayCommand]
    void InsertMessageElementAbove(UplinkMessageElementViewModel element)
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

    [RelayCommand]
    void ClearMessageElement(UplinkMessageElementViewModel element)
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
