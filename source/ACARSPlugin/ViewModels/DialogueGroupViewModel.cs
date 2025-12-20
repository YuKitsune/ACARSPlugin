using System.Collections.ObjectModel;
using ACARSPlugin.Model;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ACARSPlugin.ViewModels;

public partial class DialogueGroupViewModel : ObservableObject
{
    [ObservableProperty]
    private string callsign = string.Empty;

    [ObservableProperty]
    private ObservableCollection<MessageViewModel> messages = [];

    [ObservableProperty]
    private DateTimeOffset firstMessageTime;
}
