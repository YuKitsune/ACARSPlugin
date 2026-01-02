using CPDLCPlugin.Configuration;
using CPDLCPlugin.ViewModels;
using CPDLCPlugin.Windows;
using MediatR;

namespace CPDLCPlugin.Messages;

public record OpenCurrentMessagesWindowRequest : IRequest;

public class OpenCurrentMessagesWindowRequestHandler(
    WindowManager windowManager,
    PluginConfiguration pluginConfiguration,
    IMediator mediator,
    DialogueStore dialogueStore,
    IErrorReporter errorReporter,
    IGuiInvoker guiInvoker,
    IJurisdictionChecker jurisdictionChecker)
    : IRequestHandler<OpenCurrentMessagesWindowRequest>
{
    public Task Handle(OpenCurrentMessagesWindowRequest request, CancellationToken cancellationToken)
    {
        windowManager.FocusOrCreateWindow(
            WindowKeys.CurrentMessages,
            windowHandle =>
            {
                var viewModel = new CurrentMessagesViewModel(
                    pluginConfiguration,
                    dialogueStore,
                    mediator,
                    guiInvoker,
                    errorReporter,
                    windowHandle,
                    jurisdictionChecker);

                return new CurrentMessagesWindow(viewModel);
            });

        return Task.CompletedTask;
    }
}
