namespace AiCodeReview.Domain.Common;

/// <summary>
/// A validation failure carrying per-field detail, so the API can populate the
/// <c>errors</c> member of a ProblemDetails response without a second lookup.
/// </summary>
public sealed record ValidationError : Error
{
    public const string DefaultCode = "validation.failed";

    public ValidationError(IReadOnlyDictionary<string, string[]> failures)
        : base(DefaultCode, "One or more validation errors occurred.", ErrorType.Validation)
    {
        ArgumentNullException.ThrowIfNull(failures);
        Failures = failures;
    }

    public IReadOnlyDictionary<string, string[]> Failures { get; }
}
