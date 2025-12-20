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

        // If already on UI thread, execute directly to avoid deadlock
        if (!mainForm.InvokeRequired)
        {
            action();
            return;
        }
        
        MMI.InvokeOnGUI(delegate { action(); });
    }
}