using AiCodeReview.Domain.Common;

namespace AiCodeReview.UnitTests.Domain.Common;

public sealed class EntityTests
{
    private sealed class Repository(Guid id) : Entity(id);

    private sealed class PullRequest(Guid id) : Entity(id);

    [Fact]
    public void Entities_of_the_same_type_and_id_are_equal()
    {
        var id = Guid.CreateVersion7();

        new Repository(id).ShouldBe(new Repository(id));
        new Repository(id).GetHashCode().ShouldBe(new Repository(id).GetHashCode());
    }

    [Fact]
    public void Entities_of_different_types_are_never_equal()
    {
        var id = Guid.CreateVersion7();

        new Repository(id).Equals(new PullRequest(id)).ShouldBeFalse();
    }

    [Fact]
    public void Transient_entities_are_not_equal()
    {
        // Two unsaved entities both have an empty id. Treating them as equal
        // would silently collapse them inside a HashSet or EF change tracker.
        new Repository(Guid.Empty).Equals(new Repository(Guid.Empty)).ShouldBeFalse();
    }
}
