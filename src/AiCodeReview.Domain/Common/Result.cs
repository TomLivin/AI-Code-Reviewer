namespace AiCodeReview.Domain.Common;

/// <summary>
/// Represents the outcome of an operation that can fail for expected reasons.
/// Expected failures are values; exceptions are reserved for defects and
/// infrastructure faults. See ADR-003.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot carry an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failed result must carry an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    /// <summary>
    /// Returns the first failure in <paramref name="results"/>, or success when
    /// they all succeeded. Useful for validating several inputs at once.
    /// </summary>
    public static Result FirstFailureOrSuccess(params ReadOnlySpan<Result> results)
    {
        foreach (Result result in results)
        {
            if (result.IsFailure)
            {
                return result;
            }
        }

        return Success();
    }
}
