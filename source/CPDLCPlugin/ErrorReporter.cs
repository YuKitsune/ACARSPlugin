namespace CPDLCPlugin;

public interface IErrorReporter
{
    void ReportError(Exception ex);
    void ReportError(Exception ex, string message);
}

public class ErrorReporter : IErrorReporter
{
    public void ReportError(Exception exception)
    {
        Plugin.AddError(exception);
    }

    public void ReportError(Exception exception, string message)
    {
        Plugin.AddError(exception, message);
    }
}