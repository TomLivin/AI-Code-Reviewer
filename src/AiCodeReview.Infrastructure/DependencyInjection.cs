using AiCodeReview.Application.Abstractions.Persistence;
using AiCodeReview.Infrastructure.Configuration;
using AiCodeReview.Infrastructure.Persistence;
using AiCodeReview.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AiCodeReview.Infrastructure;

/// <summary>
/// Registers the concrete adapters that satisfy Application abstractions.
/// Only a composition root may call this.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Tag used by the readiness probe; liveness deliberately excludes dependencies.</summary>
    public const string ReadinessTag = "ready";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(
                static options => options.MaxRetryCount >= 0 && options.CommandTimeoutSeconds > 0,
                $"{DatabaseOptions.SectionName} retry count must not be negative and command timeout must be positive.")
            .ValidateOnStart();

        string connectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{DatabaseOptions.ConnectionStringName}' is not configured. "
                + $"Set ConnectionStrings:{DatabaseOptions.ConnectionStringName} in configuration "
                + $"or the ConnectionStrings__{DatabaseOptions.ConnectionStringName} environment variable.");

        services.AddSingleton<AuditableEntityInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, builder) =>
        {
            DatabaseOptions options = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            builder.UseNpgsql(connectionString, npgsql =>
            {
                // Managed PostgreSQL drops connections during failover and
                // maintenance; without this a routine restart surfaces as a
                // failed review rather than a retried one.
                npgsql.EnableRetryOnFailure(
                    options.MaxRetryCount,
                    TimeSpan.FromSeconds(options.MaxRetryDelaySeconds),
                    errorCodesToAdd: null);

                npgsql.CommandTimeout(options.CommandTimeoutSeconds);
                npgsql.MigrationsHistoryTable(AppDbContext.MigrationsHistoryTable);
            });

            // PostgreSQL folds unquoted identifiers to lower case, so PascalCase
            // names would have to be quoted in every hand-written query.
            builder.UseSnakeCaseNamingConvention();

            builder.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());

            builder.EnableSensitiveDataLogging(options.EnableSensitiveDataLogging);
            builder.EnableDetailedErrors(options.EnableDetailedErrors);
        });

        services.AddScoped<IAppDbContext>(serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(
                name: "database",
                tags: [ReadinessTag]);

        return services;
    }
}
