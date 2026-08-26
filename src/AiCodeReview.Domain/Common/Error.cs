namespace AiCodeReview.Domain.Common;

/// <summary>
/// A failure value. <see cref="Code"/> is a stable machine-readable identifier
/// (for example <c>github.rate_limited</c>); <see cref="Message"/> is safe to
/// return to a caller and must never contain secrets or internal detail.
/// </summary>
public record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Unexpected);

    public Error(string code, string message, ErrorType type)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(message);

        Code = code;
        Message = message;
        Type = type;
    }

    public string Code { get; }

    public string Message { get; }

    public ErrorType Type { get; }

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    public static Error External(string code, string message) => new(code, message, ErrorType.External);

    public static Error Unexpected(string code, string message) => new(code, message, ErrorType.Unexpected);
}
