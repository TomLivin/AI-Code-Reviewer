using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AiCodeReview.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef` build the model without starting the API or the Worker, so
/// migrations are generated from this project alone and a migration job in a
/// container needs no host configuration.
/// </summary>
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// Placeholder only. Generating a migration needs the provider to produce
    /// SQL, not a reachable server, so nothing connects with this. Applying a
    /// migration overrides it through the environment variable below.
    /// </summary>
    private const string DesignTimePlaceholder =
        "Host=localhost;Database=aicodereview_designtime;Username=designtime;Password=designtime";

    private const string ConnectionStringVariable = "ConnectionStrings__AppDb";

    public AppDbContext CreateDbContext(string[] args)
    {
        string connectionString =
            Environment.GetEnvironmentVariable(ConnectionStringVariable) is { Length: > 0 } configured
                ? configured
                : DesignTimePlaceholder;

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(AppDbContext.MigrationsHistoryTable))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}
