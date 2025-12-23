using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ACARSPlugin.ViewModels;

namespace ACARSPlugin.Windows;

public partial class HistoryWindow : Window
{
    private FrameworkElement? _extendedMessageAnchor;
    private readonly HistoryViewModel _viewModel;
    private double _lockedWidth;

    public HistoryWindow(HistoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _lockedWidth = Width;

        Closed += (_, _) => _viewModel.Dispose();
        LocationChanged += (_, _) => CollapseExtendedMessage();
        SizeChanged += Window_SizeChanged;
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Lock width, only allow height resizing
        if (Math.Abs(e.NewSize.Width - _lockedWidth) > 0.1)
        {
            Width = _lockedWidth;
        }

        // Close extended message on resize
        if (e.HeightChanged || e.WidthChanged)
        {
            CollapseExtendedMessage();
        }
    }

    private void CallsignButton_Click(object sender, RoutedEventArgs e)
    {
        CallsignButton.Visibility = Visibility.Collapsed;
        CallsignTextBox.Visibility = Visibility.Visible;
        CallsignTextBox.Focus();
        CallsignTextBox.SelectAll();
    }

    private void CallsignTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        
        _viewModel.LoadMessagesCommand.Execute(null);
        CallsignTextBox.Visibility = Visibility.Collapsed;
        CallsignButton.Visibility = Visibility.Visible;

        e.Handled = true;
    }

    private void Message_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: HistoryMessageViewModel messageViewModel } element ||
            DataContext is not HistoryViewModel viewModel)
            return;

        _extendedMessageAnchor = element;
        ExtendedMessagePopup.PlacementTarget = _extendedMessageAnchor;

        viewModel.ToggleExtendedDisplayCommand.Execute(messageViewModel);
        ExtendedMessagePopup.IsOpen = viewModel.CurrentlyExtendedMessage != null;

        e.Handled = true;
    }

    private void ExtendedMessage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        CollapseExtendedMessage();
        e.Handled = true;
    }

    private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
        {
            CollapseExtendedMessage();
        }
    }

    private void CollapseExtendedMessage()
    {
        if (DataContext is not HistoryViewModel viewModel ||
            viewModel.CurrentlyExtendedMessage == null)
            return;

        viewModel.ToggleExtendedDisplayCommand.Execute(viewModel.CurrentlyExtendedMessage);
        ExtendedMessagePopup.IsOpen = false;
    }
}