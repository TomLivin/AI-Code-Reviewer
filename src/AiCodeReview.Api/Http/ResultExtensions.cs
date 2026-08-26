using AiCodeReview.Domain.Common;

namespace AiCodeReview.Api.Http;

/// <summary>
/// The single place where an Application-layer <see cref="Result"/> becomes an
/// HTTP response. Endpoints never choose status codes themselves, so the
/// mapping stays consistent across the whole API.
/// </summary>
public static class ResultExtensions
{
    private const string ErrorCodeExtension = "errorCode";

    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : result.Error.ToProblem();

    public static IResult ToHttpResult<TValue>(this Result<TValue> result) =>
        result.ToHttpResult(static value => Results.Ok(value));

    public static IResult ToHttpResult<TValue>(this Result<TValue> result, Func<TValue, IResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result.IsSuccess ? onSuccess(result.Value) : result.Error.ToProblem();
    }

    public static IResult ToProblem(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (error is ValidationError validationError)
        {
            return Results.ValidationProblem(
                errors: validationError.Failures.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal),
                detail: validationError.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation failed",
                type: ApiConstants.ProblemTypes.Validation,
                extensions: BuildExtensions(error));
        }

        return Results.Problem(
            detail: error.Message,
            statusCode: ToStatusCode(error.Type),
            title: ToTitle(error.Type),
            type: ToProblemType(error.Type),
            extensions: BuildExtensions(error));
    }

    private static Dictionary<string, object?> BuildExtensions(Error error) =>
        new(StringComparer.Ordinal) { [ErrorCodeExtension] = error.Code };

    private static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.External => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string ToProblemType(ErrorType type) => type switch
    {
        ErrorType.Validation => ApiConstants.ProblemTypes.Validation,
        ErrorType.Unauthorized => ApiConstants.ProblemTypes.Unauthorized,
        ErrorType.Forbidden => ApiConstants.ProblemTypes.Forbidden,
        ErrorType.NotFound => ApiConstants.ProblemTypes.NotFound,
        ErrorType.Conflict => ApiConstants.ProblemTypes.Conflict,
        ErrorType.External => ApiConstants.ProblemTypes.External,
        _ => ApiConstants.ProblemTypes.Unexpected
    };

    private static string ToTitle(ErrorType type) => type switch
    {
        ErrorType.Validation => "Validation failed",
        ErrorType.Unauthorized => "Authentication required",
        ErrorType.Forbidden => "Access denied",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Conflict => "Conflicting request",
        ErrorType.External => "Upstream dependency failed",
        _ => "An unexpected error occurred."
    };
}
