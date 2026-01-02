using System.Windows;
using System.Windows.Forms;

namespace CPDLCPlugin;

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

public class FormWindowHandle(Form? form = null) : IWindowHandle
{
    Form? Form { get; set; } = form;

    public void SetForm(Form form)
    {
        Form = form;
    }

    public void Close()
    {
        Form?.Close();
    }
}
