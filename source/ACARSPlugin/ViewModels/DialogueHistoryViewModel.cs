using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ACARSPlugin.ViewModels;

public partial class DialogueHistoryViewModel : ObservableObject
{
    [ObservableProperty]
    ObservableCollection<HistoryMessageViewModel> messages = [];

    [ObservableProperty]
    DateTimeOffset firstMessageTime;
}
