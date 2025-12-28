using System.Text;
using System.Windows.Media;
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
        MaxCharacters = 250; // TODO: Calculate based on view width
        DisplayText = GetDisplayText(Message, Received, StandbySent, Deferred, MaxCharacters);
        FullDisplayText = GetDisplayText(Message, Received, StandbySent, Deferred, int.MaxValue);
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
        MaxCharacters = 250; // TODO: Calculate based on view width
        DisplayText = GetDisplayText(Message, Received, StandbySent, Deferred, MaxCharacters);
    }
#endif

    public DownlinkMessage OriginalMessage { get; }

    [ObservableProperty] DateTimeOffset received;
    [ObservableProperty] bool standbySent;
    [ObservableProperty] bool deferred;
    [ObservableProperty] string message;
    [ObservableProperty] string displayText;
    [ObservableProperty] string fullDisplayText;
    [ObservableProperty] int maxCharacters;

    static string GetDisplayText(string fullContent, DateTimeOffset received, bool standbySent, bool requestDeferred, int maxCharacters)
    {
        var sb = new StringBuilder();

        sb.Append(" ");

        if (requestDeferred)
        {
            sb.Append("D");
        }
        else if (standbySent)
        {
            sb.Append("S");
        }

        sb.Append(" ");

        sb.Append($"{received:HH:mm}");

        sb.Append(" ");

        var remainingLength = maxCharacters - sb.Length;
        if (fullContent.Length > remainingLength)
        {
            sb.Append(fullContent.Substring(0, remainingLength));
        }
        else
        {
            sb.Append(fullContent);
        }

        var totalLength = sb.Length + fullContent.Length;
        if (totalLength > maxCharacters)
        {
            sb[0] = '*';
        }

        return sb.ToString();
    }
}
