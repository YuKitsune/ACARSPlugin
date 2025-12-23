using System.Collections.ObjectModel;
using ACARSPlugin.Model;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ACARSPlugin.ViewModels;

public partial class DialogueViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<CurrentMessageViewModel> messages = [];

    [ObservableProperty]
    private DateTimeOffset firstMessageTime;
}