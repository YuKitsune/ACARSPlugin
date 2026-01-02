using ACARSPlugin.Configuration;
using ACARSPlugin.ViewModels;
using ACARSPlugin.Windows;
using MediatR;

namespace ACARSPlugin.Messages;

public record OpenCurrentMessagesWindowRequest : IRequest;

public class OpenCurrentMessagesWindowRequestHandler(
    WindowManager windowManager,
    PluginConfiguration pluginConfiguration,
    IMediator mediator,
    DialogueStore dialogueStore,
    IErrorReporter errorReporter,
    IGuiInvoker guiInvoker)
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
                    windowHandle);

                return new CurrentMessagesWindow(viewModel);
            });

        return Task.CompletedTask;
    }
}
