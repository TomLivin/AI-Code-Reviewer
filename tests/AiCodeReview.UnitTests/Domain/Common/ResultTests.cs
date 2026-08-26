using AiCodeReview.Domain.Common;

namespace AiCodeReview.UnitTests.Domain.Common;

public sealed class ResultTests
{
    private static readonly Error SampleError = Error.NotFound("repository.not_found", "Repository was not found.");

    [Fact]
    public void Success_carries_no_error()
    {
        Result result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void Failure_carries_the_error()
    {
        Result result = Result.Failure(SampleError);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("repository.not_found");
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public void Failure_without_an_error_is_rejected()
    {
        // Guards the invariant that a failure is always explainable; a failed
        // result with no error would produce a 500 with an empty body.
        Should.Throw<InvalidOperationException>(() => Result.Failure(Error.None));
    }

    [Fact]
    public void Reading_the_value_of_a_failed_result_throws()
    {
        Result<string> result = Result.Failure<string>(SampleError);

        var exception = Should.Throw<InvalidOperationException>(() => result.Value);
        exception.Message.ShouldContain("repository.not_found");
    }

    [Fact]
    public void A_value_implicitly_becomes_a_successful_result()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void An_error_implicitly_becomes_a_failed_result()
    {
        Result<int> result = SampleError;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void Map_transforms_a_success_and_short_circuits_a_failure()
    {
        Result.Success(21).Map(value => value * 2).Value.ShouldBe(42);

        Result<int> failed = Result.Failure<int>(SampleError);
        Result<int> mapped = failed.Map(value => value * 2);

        mapped.IsFailure.ShouldBeTrue();
        mapped.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void Match_selects_the_branch_matching_the_outcome()
    {
        Result.Success("ok").Match(value => value, error => error.Code).ShouldBe("ok");
        Result.Failure<string>(SampleError).Match(value => value, error => error.Code).ShouldBe("repository.not_found");
    }

    [Fact]
    public void FirstFailureOrSuccess_returns_the_earliest_failure()
    {
        Error second = Error.Validation("pr.number_invalid", "Pull request number must be positive.");

        Result outcome = Result.FirstFailureOrSuccess(
            Result.Success(),
            Result.Failure(SampleError),
            Result.Failure(second));

        outcome.Error.ShouldBe(SampleError);

        Result.FirstFailureOrSuccess(Result.Success(), Result.Success()).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ValidationError_exposes_per_field_failures()
    {
        var failures = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["number"] = ["Must be greater than zero."]
        };

        var error = new ValidationError(failures);

        error.Type.ShouldBe(ErrorType.Validation);
        error.Code.ShouldBe(ValidationError.DefaultCode);
        error.Failures["number"].ShouldContain("Must be greater than zero.");
    }
}
