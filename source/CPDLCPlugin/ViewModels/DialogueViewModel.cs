using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CPDLCPlugin.ViewModels;

public partial class DialogueViewModel : ObservableObject
{
    [ObservableProperty]
    ObservableCollection<CurrentMessageViewModel> messages = [];

    [ObservableProperty]
    DateTimeOffset firstMessageTime;
}
