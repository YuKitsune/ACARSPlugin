using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ACARSPlugin.Configuration;
using ACARSPlugin.Messages;
using ACARSPlugin.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace ACARSPlugin.Windows;

public partial class EditorWindow : Window, IRecipient<DisconnectedNotification>
{
    public EditorWindow(EditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Register for disconnect notifications
        WeakReferenceMessenger.Default.Register<DisconnectedNotification>(this);
    }

    public void Receive(DisconnectedNotification message)
    {
        // Close the window when disconnected from the server
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Unregister from notifications when window closes
        WeakReferenceMessenger.Default.Unregister<DisconnectedNotification>(this);
        base.OnClosed(e);
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

    private void TemplatePartButton_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not UplinkMessageTemplatePartViewModel templatePart)
            return;

        switch (e.ChangedButton)
        {
            case MouseButton.Left:
                // Left click: Start editing
                templatePart.IsEditing = true;
                break;

            case MouseButton.Middle:
                // Middle click: Clear the value
                templatePart.Value = null;
                e.Handled = true;
                break;
        }
    }

    private void TemplatePartTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        // Only focus when becoming visible
        if (e.NewValue is bool isVisible && isVisible)
        {
            textBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                textBox.Focus();
                Keyboard.Focus(textBox);
                textBox.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private void TemplatePartTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not UplinkMessageTemplatePartViewModel templatePart)
            return;

        // Enter: Save and exit editing
        if (e.Key == Key.Enter)
        {
            templatePart.IsEditing = false;
            e.Handled = true;
        }
        // Escape: Cancel editing (revert to original value would require storing it)
        else if (e.Key == Key.Escape)
        {
            templatePart.IsEditing = false;
            e.Handled = true;
        }
    }

    private void TemplatePartTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not UplinkMessageTemplatePartViewModel templatePart)
            return;

        templatePart.IsEditing = false;
    }
}