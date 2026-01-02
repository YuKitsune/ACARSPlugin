using System.Windows.Forms;
using vatsys;

namespace CPDLCPlugin;

public interface IGuiInvoker
{
    void InvokeOnGUI(Action<Form> action);
}

public class GuiInvoker : IGuiInvoker
{
    public void InvokeOnGUI(Action<Form> action)
    {
        var mainForm = Application.OpenForms["MainForm"];
        if (mainForm == null)
            return;

        // Check if the window handle is valid before invoking
        if (mainForm.IsDisposed || !mainForm.IsHandleCreated)
            return;

        // If already on UI thread, execute directly to avoid deadlock
        if (!mainForm.InvokeRequired)
        {
            action(mainForm);
            return;
        }

        try
        {
            MMI.InvokeOnGUI(delegate { action(mainForm); });
        }
        catch (ObjectDisposedException)
        {
            // Control was disposed during invocation - ignore during shutdown
        }
        catch (InvalidOperationException)
        {
            // Window handle was destroyed during invocation - ignore during shutdown
        }
    }
}