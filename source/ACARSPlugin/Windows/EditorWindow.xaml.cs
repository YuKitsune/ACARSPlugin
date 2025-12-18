using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ACARSPlugin.Configuration;
using ACARSPlugin.ViewModels;

namespace ACARSPlugin.Windows;

public partial class EditorWindow : Window
{
    public EditorWindow(EditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void MessageClassElement_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not UplinkMessageTemplate template ||
            DataContext is not EditorViewModel viewModel)
            return;

        viewModel.AddMessageElementCommand.Execute(template);
        e.Handled = true;
    }

    private void LineNumberButton_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not UplinkMessageElementViewModel messageElement ||
            DataContext is not EditorViewModel viewModel)
            return;

        switch (e.ChangedButton)
        {
            case MouseButton.Left:
                viewModel.ToggleMessageElementSelectionCommand.Execute(messageElement);
                break;

            case MouseButton.Right:
                viewModel.InsertMessageElementAboveCommand.Execute(messageElement);
                break;

            case MouseButton.Middle:
                viewModel.ClearMessageElementCommand.Execute(messageElement);
                break;
        }

        // Don't mark as handled so button gets visual feedback
    }

}