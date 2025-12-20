using ACARSPlugin.Model;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ACARSPlugin.ViewModels;

public partial class DownlinkMessageViewModel : ObservableObject
{
    public DownlinkMessageViewModel(DownlinkMessage message, bool standbySent = false, bool deferred = false)
    {
        OriginalMessage = message;
        Received = message.Received;
        StandbySent = standbySent;
        Deferred = deferred;
        Message = message.Content;
    }

#if DEBUG
    // Test constructor
    public DownlinkMessageViewModel()
    {
        OriginalMessage = null!;
        Received = DateTimeOffset.Now;
        StandbySent = true;
        Deferred = false;
        Message = "EXAMPLE";
        Selected = false;
    }
#endif

    public DownlinkMessage OriginalMessage { get; }
    
    [ObservableProperty]
    private DateTimeOffset received;

    [ObservableProperty]
    private bool standbySent;

    [ObservableProperty]
    private bool deferred;

    [ObservableProperty]
    private string message;

    [ObservableProperty]
    private bool selected;
}
