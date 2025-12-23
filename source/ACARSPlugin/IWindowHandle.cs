using System.Windows;

namespace ACARSPlugin;

public interface IWindowHandle
{
    void Close();
}

public class WpfWindowHandle : IWindowHandle
{
    Window? Window { get; set; }

    public void SetWindow(Window window)
    {
        Window = window;
    }
    
    public void Close()
    {
        Window?.Close();
    }
}