namespace AiCodeReview.Domain.Common;

/// <summary>
/// Classifies a failure so that transport layers can translate it without
/// the Domain or Application knowing anything about HTTP.
/// </summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    Unauthorized,
    External,
    Unexpected
}
