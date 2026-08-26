using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiCodeReview.Infrastructure;

/// <summary>
/// Registers the concrete adapters that satisfy Application abstractions:
/// persistence, GitHub access, the job queue and secret protection.
/// Only a composition root may call this.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services;
    }
}
