using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms.VisualStyles;

namespace ACARSPlugin.ViewModels;

public abstract class ViewModel : INotifyPropertyChanged, INotifyPropertyChanging
{
    readonly Dictionary<string, object?> _values = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;

    protected T? Get<T>([CallerMemberName] string propertyName = "")
    {
        if (_values.TryGetValue(propertyName, out var value))
        {
            return (T?)value;
        }
        return default;
    }

    protected void Set<T>(T value, [CallerMemberName] string propertyName = "")
    {
        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        _values[propertyName] = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class EditorViewModel : ViewModel
{
    public EditorViewModel()
    {
        DownlinkMessages =
        [
            new DownlinkMessageViewModel
            {
                Received = DateTimeOffset.Now,
                Message = "STINKY POO POO",
                Deferred = true
            },

            new DownlinkMessageViewModel
            {
                Received = DateTimeOffset.Now,
                Message = "REQUEST CLIMB UP YOUR ASS",
                Selected = true
            }
        ];

        SelectedMessageClassElements =
        [
            "WHEN CAN YOU ACCEPT [LEVEL]",
            "CAN YOU ACCEPT [LEVEL] AT [POSITION]",
            "CAN YOU ACCEPT [LEVEL] AT [TIME]",
            "MAINTAIN [LEVEL]",
            "CLIMB TO AND MAINTAIN [LEVEL]",
            "CLIMB VIA SID TO [LEVEL]"
        ];

        // Sample constructed message elements
        ConstructedMessageElements =
        [
            new UplinkMessageElementViewModel
            {
                LineNumber = 1,
                Parts =
                [
                    new UplinkMessageTextPartViewModel { Value = "CLIMB TO AND MAINTAIN " },
                    new UplinkMessageTemplatePartViewModel { Placeholder = "LEVEL", Value = "FL350", IsValid = true }
                ]
            },
            new UplinkMessageElementViewModel
            {
                LineNumber = 2,
                Parts =
                [
                    new UplinkMessageTextPartViewModel { Value = "AT " },
                    new UplinkMessageTemplatePartViewModel { Placeholder = "POSITION", Value = null, IsValid = false }
                ]
            }
        ];
    }

    public string Callsign
    {
        get => Get<string>() ?? "Unknown";
        set => Set(value);
    }
    
    public List<DownlinkMessageViewModel> DownlinkMessages
    {
        get => Get<List<DownlinkMessageViewModel>>() ?? new List<DownlinkMessageViewModel>();
        set => Set(value);
    }

    public DownlinkMessageViewModel? SelectedDownlinkMessage
    {
        get => Get<DownlinkMessageViewModel>();
        set => Set(value);
    }

    public List<string> MessageClasses
    {
        get => Get<List<string>>() ?? new List<string>();
        set => Set(value);
    }

    public string? SelectedMessageClass
    {
        get => Get<string>();
        set => Set(value);
    }

    public List<string> SelectedMessageClassElements
    {
        get => Get<List<string>>() ?? new List<string>();
        set => Set(value);
    }

    public List<UplinkMessageElementViewModel> ConstructedMessageElements
    {
        get => Get<List<UplinkMessageElementViewModel>>() ?? new List<UplinkMessageElementViewModel>();
        set => Set(value);
    }

    public string? Error
    {
        get => Get<string>();
        set => Set(value);
    }
}

public class UplinkMessageElementViewModel : ViewModel
{
    public UplinkMessageElementViewModel()
    {
        Parts = new List<IUplinkMessagePartViewModel>();
    }

    public int LineNumber
    {
        get => Get<int>();
        set => Set(value);
    }

    public List<IUplinkMessagePartViewModel> Parts
    {
        get => Get<List<IUplinkMessagePartViewModel>>() ?? new List<IUplinkMessagePartViewModel>();
        set => Set(value);
    }
}

public interface IUplinkMessagePartViewModel;

public class UplinkMessageTextPartViewModel : ViewModel, IUplinkMessagePartViewModel
{
    public string Value
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }
}

public class UplinkMessageTemplatePartViewModel : ViewModel, IUplinkMessagePartViewModel
{
    public string Placeholder
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }

    public string? Value
    {
        get => Get<string?>();
        set => Set(value);
    }

    public bool IsValid
    {
        get => Get<bool>();
        set => Set(value);
    }
}
