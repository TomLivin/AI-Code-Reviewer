using AiCodeReview.Infrastructure.Persistence;

namespace AiCodeReview.IntegrationTests;

[Collection(PostgresCollection.Name)]
public abstract class DatabaseTestBase(PostgresFixture fixture)
{
    protected PostgresFixture Fixture { get; } = fixture;

    /// <summary>
    /// Skips rather than fails when no container could be started, so a machine
    /// without Docker reports honestly instead of showing red tests.
    /// </summary>
    protected AppDbContext NewContext()
    {
        Assert.SkipWhen(Fixture.SkipReason is not null, Fixture.SkipReason ?? string.Empty);

        return Fixture.CreateContext();
    }
}
