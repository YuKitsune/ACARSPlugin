using vatsys;

namespace ACARSPlugin;

public interface IErrorReporter
{
    void ReportError(Exception ex);
}

public class ErrorReporter : IErrorReporter
{
    public void ReportError(Exception exception)
    {
        Errors.Add(exception, Plugin.Name);
    }
}