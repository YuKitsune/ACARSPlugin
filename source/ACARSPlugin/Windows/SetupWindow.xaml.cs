using System.Windows;
using ACARSPlugin.ViewModels;

namespace ACARSPlugin.Windows;

public partial class SetupWindow : Window
{
    public SetupWindow(SetupViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
