using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        // Dispose the view model to unregister from message updates
        if (DataContext is EditorViewModel viewModel)
        {
            viewModel.Dispose();
        }

        base.OnClosed(e);
    }

    void MessageClassElement_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not UplinkMessageTemplateViewModel template ||
            DataContext is not EditorViewModel viewModel)
            return;

        viewModel.AddMessageElementCommand.Execute(template);
        e.Handled = true;
    }

    void LineNumberButton_Click(object sender, MouseButtonEventArgs e)
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

    void TemplatePartButton_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not UplinkMessageTemplateElementComponentViewModel templatePart)
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

    void TemplatePartTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
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

    void TemplatePartTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not UplinkMessageTemplateElementComponentViewModel templatePart)
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

    void TemplatePartTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not UplinkMessageTemplateElementComponentViewModel templatePart)
            return;

        templatePart.IsEditing = false;
    }

    void DownlinkMessage_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not DownlinkMessageViewModel clickedMessage ||
            DataContext is not EditorViewModel viewModel)
            return;

        // Toggle selection: if clicking on the already selected message, deselect it
        if (viewModel.SelectedDownlinkMessage == clickedMessage)
        {
            viewModel.SelectedDownlinkMessage = null;
        }
        else
        {
            viewModel.SelectedDownlinkMessage = clickedMessage;
        }

        e.Handled = true;
    }

    void DownlinkMessage_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not DownlinkMessageViewModel downlinkMessage ||
            DataContext is not EditorViewModel viewModel)
            return;

        ExtendedDownlinkMessagePopup.PlacementTarget = element;
        viewModel.CurrentlyExtendedDownlinkMessage = downlinkMessage;
        ExtendedDownlinkMessagePopup.IsOpen = true;

        e.Handled = true;
    }

    void ExtendedDownlinkMessagePopup_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Close the popup on any click (left or right)
        ExtendedDownlinkMessagePopup.IsOpen = false;
        e.Handled = true;
    }

    void ExtendedDownlinkMessagePopup_Closed(object? sender, EventArgs e)
    {
        // Clear the extended message in the ViewModel when the popup closes
        if (DataContext is not EditorViewModel viewModel)
            return;

        viewModel.CurrentlyExtendedDownlinkMessage = null;
    }
}
