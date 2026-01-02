using CommunityToolkit.Mvvm.ComponentModel;
using CPDLCPlugin.Configuration;

namespace CPDLCPlugin.ViewModels;

public partial class UplinkMessageElementViewModel : ObservableObject
{
    public UplinkMessageElementViewModel()
    {
        Parts = [];
        ResponseType = UplinkResponseType.NoResponse;
        Prefix = GetPrefix(false, false);
    }

    public UplinkMessageElementViewModel(IUplinkMessageElementComponentViewModel[] parts, UplinkResponseType responseType)
    {
        Parts = parts;
        ResponseType = responseType;
        Prefix = GetPrefix(false, false);
    }

    [ObservableProperty] IUplinkMessageElementComponentViewModel[] parts = [];
    [ObservableProperty] UplinkResponseType responseType = UplinkResponseType.NoResponse;

    // TODO: Make these message types and indicators configurable
    [ObservableProperty] bool isFreeText;
    [ObservableProperty] bool isRevision;

    [ObservableProperty] string prefix;

    public bool IsEmpty => Parts.Length == 0;

    public void Replace(IUplinkMessageElementComponentViewModel[] parts, UplinkResponseType responseType)
    {
        Parts = parts;
        ResponseType = responseType;
    }

    public void Clear()
    {
        Parts = [];
        ResponseType =  UplinkResponseType.NoResponse;
    }

    static string GetPrefix(bool isFreeText, bool isRevision)
    {
        if (isRevision)
        {
            return "R:";
        }

        if (isFreeText)
        {
            return "F:";
        }

        return "  ";
    }
}
