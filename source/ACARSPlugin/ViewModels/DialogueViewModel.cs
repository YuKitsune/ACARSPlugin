using System.Collections.ObjectModel;
using ACARSPlugin.Model;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ACARSPlugin.ViewModels;

public partial class DialogueViewModel : ObservableObject
{
    [ObservableProperty]
    ObservableCollection<CurrentMessageViewModel> messages = [];

    [ObservableProperty]
    DateTimeOffset firstMessageTime;
}
