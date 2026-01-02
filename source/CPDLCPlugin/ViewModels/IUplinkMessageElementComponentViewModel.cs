using CommunityToolkit.Mvvm.ComponentModel;

namespace CPDLCPlugin.ViewModels;

public interface IUplinkMessageElementComponentViewModel;

public partial class UplinkMessageTextElementComponentViewModel : ObservableObject, IUplinkMessageElementComponentViewModel
{
    [ObservableProperty]
    string value = string.Empty;

    public UplinkMessageTextElementComponentViewModel(string value)
    {
        Value = value;
    }
}

public partial class UplinkMessageTemplateElementComponentViewModel : ObservableObject, IUplinkMessageElementComponentViewModel
{
    [ObservableProperty]
    string placeholder = string.Empty;

    [ObservableProperty]
    string? value;

    [ObservableProperty]
    bool isEditing;

    public UplinkMessageTemplateElementComponentViewModel(string placeholder)
    {
        Placeholder = placeholder;
        Value = string.Empty;
        IsEditing = false;
    }
}
