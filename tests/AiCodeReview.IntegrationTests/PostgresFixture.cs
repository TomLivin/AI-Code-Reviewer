using AiCodeReview.Infrastructure.Persistence;
using AiCodeReview.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace AiCodeReview.IntegrationTests;

/// <summary>
/// A disposable PostgreSQL instance per test run, with migrations applied.
///
/// The EF in-memory provider is deliberately not used: it enforces no unique
/// index, no partial index, no foreign key and no check constraint, so it
/// cannot verify the invariants this schema encodes. Testing against anything
/// other than real PostgreSQL would make these tests theatre.
///
/// When Docker is unavailable the fixture records why and the tests skip rather
/// than fail, so the suite stays usable on a machine without Docker while still
/// running for real in CI.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string PostgresImage = "postgres:17-alpine";

    // Built inside InitializeAsync rather than in a field initialiser: the
    // builder resolves the Docker endpoint eagerly, so constructing it here
    // would throw before the guarded path below could turn that into a skip.
    private PostgreSqlContainer? _container;

    public string? SkipReason { get; private set; }

    public async ValueTask InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder(PostgresImage)
                .WithDatabase("aicodereview_test")
                // Credentials for a throwaway container that lives only for this run.
                .WithUsername("aicodereview_test")
                .WithPassword("aicodereview_test")
                .Build();

            await _container.StartAsync();

            await using AppDbContext context = CreateContext();
            await context.Database.MigrateAsync();
        }
        catch (Exception exception)
        {
            _container = null;
            SkipReason =
                "No PostgreSQL container could be started, so these tests were skipped. "
                + "Install and start Docker Desktop to run them. "
                + $"Underlying error: {exception.GetType().Name}: {exception.Message}";
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public AppDbContext CreateContext()
    {
        if (_container is null)
        {
            throw new InvalidOperationException(SkipReason ?? "The PostgreSQL container is not running.");
        }

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new AuditableEntityInterceptor(TimeProvider.System))
            .Options;

        return new AppDbContext(options);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
