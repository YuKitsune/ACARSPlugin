using System.Windows;
using System.Windows.Controls;
using ACARSPlugin.ViewModels;

namespace ACARSPlugin.Windows;

public partial class SetupWindow : Window
{
    public SetupWindow(SetupViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    public SetupViewModel ViewModel
    {
        get => (SetupViewModel) DataContext;
        set
        {
            DataContext = value;
            // Set initial password in PasswordBox
            // if (value != null)
            // {
            //     ApiKeyPasswordBox.Password = value.ApiKey;
            // }
        }
    }

    void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // void ApiKeyPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    // {
    //     if (sender is not PasswordBox passwordBox)
    //         return;
    //
    //     ViewModel.ApiKey = passwordBox.Password;
    // }
}
