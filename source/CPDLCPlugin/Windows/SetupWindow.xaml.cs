using System.Windows;
using CPDLCPlugin.ViewModels;

namespace CPDLCPlugin.Windows;

public partial class SetupWindow : Window
{
    public SetupWindow(SetupViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
