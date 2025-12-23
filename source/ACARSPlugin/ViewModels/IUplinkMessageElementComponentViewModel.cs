using CommunityToolkit.Mvvm.ComponentModel;

namespace ACARSPlugin.ViewModels;

public interface IUplinkMessageElementComponentViewModel;

public partial class UplinkMessageTextElementComponentViewModel : ObservableObject, IUplinkMessageElementComponentViewModel
{
    [ObservableProperty]
    private string value = string.Empty;

    public UplinkMessageTextElementComponentViewModel(string value)
    {
        Value = value;
    }
}

public partial class UplinkMessageTemplateElementComponentViewModel : ObservableObject, IUplinkMessageElementComponentViewModel
{
    [ObservableProperty]
    private string placeholder = string.Empty;

    [ObservableProperty]
    private string? value;

    [ObservableProperty]
    private bool isEditing;

    public UplinkMessageTemplateElementComponentViewModel(string placeholder)
    {
        Placeholder = placeholder;
        Value = string.Empty;
        IsEditing = false;
    }
}