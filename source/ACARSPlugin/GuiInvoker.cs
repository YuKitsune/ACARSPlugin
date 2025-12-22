using vatsys;

namespace ACARSPlugin;

public interface IGuiInvoker
{
    void InvokeOnGUI(Action action);
}

public class GuiInvoker : IGuiInvoker
{
    public void InvokeOnGUI(Action action)
    {
        var mainForm = System.Windows.Forms.Application.OpenForms["MainForm"];
        if (mainForm == null)
            return;

        // Check if the window handle is valid before invoking
        if (mainForm.IsDisposed || !mainForm.IsHandleCreated)
            return;

        // If already on UI thread, execute directly to avoid deadlock
        if (!mainForm.InvokeRequired)
        {
            action();
            return;
        }

        try
        {
            MMI.InvokeOnGUI(delegate { action(); });
        }
        catch (InvalidOperationException)
        {
            // Window handle was destroyed during invocation - ignore during shutdown
        }
    }
}