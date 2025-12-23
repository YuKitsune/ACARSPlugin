using System.Windows;
using System.Windows.Forms.Integration;

namespace ACARSPlugin.Windows;

public class WindowManager(IGuiInvoker guiInvoker)
{
    readonly IDictionary<string, Window> _windows = new Dictionary<string, Window>();

    public void FocusOrCreateWindow(
        string key,
        Func<IWindowHandle, Window> createWindow)
    {
        guiInvoker.InvokeOnGUI(mainForm =>
        {
            if (_windows.TryGetValue(key, out var existingWindowHandle))
            {
                existingWindowHandle.Focus();
                return;
            }

            var windowHandle = new WpfWindowHandle();
            var window = createWindow(windowHandle);
            windowHandle.SetWindow(window);

            ElementHost.EnableModelessKeyboardInterop(window);
            
            var helper = new System.Windows.Interop.WindowInteropHelper(window);
            helper.Owner = mainForm.Handle;
            
            window.Closed += (_, _) => _windows.Remove(key);
            window.Show();

            _windows[key] = window;
        });
    }

    public void TryRemoveWindow(string key)
    {
        if (!_windows.TryGetValue(key, out var window))
            return;
        
        window.Close();
        _windows.Remove(key);
    }
}
