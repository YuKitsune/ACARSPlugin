using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ACARSPlugin.ViewModels;

public partial class DialogueHistoryViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<HistoryMessageViewModel> messages = [];

    [ObservableProperty]
    private DateTimeOffset firstMessageTime;
}