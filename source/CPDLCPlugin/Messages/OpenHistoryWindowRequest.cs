using CPDLCPlugin.Configuration;
using CPDLCPlugin.ViewModels;
using CPDLCPlugin.Windows;
using MediatR;

namespace CPDLCPlugin.Messages;

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
