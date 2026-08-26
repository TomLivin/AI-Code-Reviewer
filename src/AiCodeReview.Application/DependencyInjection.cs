using Microsoft.Extensions.DependencyInjection;

namespace AiCodeReview.Application;

/// <summary>
/// Registers the Application layer. Hosts call this instead of knowing which
/// concrete handlers exist, so adding a use case never edits a host.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
