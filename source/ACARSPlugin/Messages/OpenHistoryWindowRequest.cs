using ACARSPlugin.Configuration;
using ACARSPlugin.ViewModels;
using ACARSPlugin.Windows;
using MediatR;

namespace ACARSPlugin.Messages;

public record OpenHistoryWindowRequest(string? Callsign = null) : IRequest;

public class OpenHistoryWindowRequestHandler(
    WindowManager windowManager,
    PluginConfiguration pluginConfiguration,
    DialogueStore dialogueStore,
    IErrorReporter errorReporter,
    IGuiInvoker guiInvoker)
    : IRequestHandler<OpenHistoryWindowRequest>
{
    public Task Handle(OpenHistoryWindowRequest request, CancellationToken cancellationToken)
    {
        windowManager.FocusOrCreateWindow(
            WindowKeys.History,
            windowHandle =>
            {
                var viewModel = new HistoryViewModel(
                    pluginConfiguration,
                    dialogueStore,
                    guiInvoker,
                    errorReporter,
                    request.Callsign);

                return new HistoryWindow(viewModel);
            });

        return Task.CompletedTask;
    }
}
