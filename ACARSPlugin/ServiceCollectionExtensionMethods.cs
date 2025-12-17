using Microsoft.Extensions.DependencyInjection;

namespace ACARSPlugin;

public static class ServiceCollectionExtensionMethods
{
    public static IServiceCollection AddViewModels(this IServiceCollection serviceCollection)
    {
        return serviceCollection;
    }
}
