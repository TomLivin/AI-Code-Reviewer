using System.Diagnostics;
using Serilog.Context;

namespace AiCodeReview.Api.Middleware;

/// <summary>
/// Assigns every request a correlation id, echoes it back to the caller and
/// attaches it to the logging scope so a single review can be traced from the
/// HTTP call through to the Worker that processes it.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string correlationId = ResolveCorrelationId(context);

        context.Response.Headers[ApiConstants.Headers.CorrelationId] = correlationId;
        Activity.Current?.SetTag(ApiConstants.Logging.CorrelationIdProperty, correlationId);

        using (LogContext.PushProperty(ApiConstants.Logging.CorrelationIdProperty, correlationId))
        {
            await next(context);
        }
    }

    /// <summary>
    /// Inbound values are accepted so a caller can stitch traces together, but
    /// only after validation: an unvalidated header would let a client inject
    /// arbitrary text into structured logs.
    /// </summary>
    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(ApiConstants.Headers.CorrelationId, out var values))
        {
            string? candidate = values.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(candidate) && IsWellFormed(candidate))
            {
                return candidate;
            }
        }

        return context.TraceIdentifier;
    }

    private static bool IsWellFormed(string value)
    {
        if (value.Length > MaxLength)
        {
            return false;
        }

        foreach (char c in value)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_' or ':' or '.'))
            {
                return false;
            }
        }

        return true;
    }
}
