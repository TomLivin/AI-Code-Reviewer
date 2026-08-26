using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AiCodeReview.Api.Extensions;

/// <summary>
/// Liveness and readiness are deliberately different questions.
/// Liveness asks "is this process running?" — a failing dependency must not
/// cause an orchestrator to kill an otherwise healthy instance.
/// Readiness asks "can this instance serve traffic?" and does check dependencies.
/// </summary>
public static class HealthCheckExtensions
{
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks();

        return services;
    }

    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks(ApiConstants.HealthChecks.LivePath, new HealthCheckOptions
        {
            Predicate = static _ => false,
            ResponseWriter = WriteResponseAsync
        });

        app.MapHealthChecks(ApiConstants.HealthChecks.ReadyPath, new HealthCheckOptions
        {
            Predicate = static registration => registration.Tags.Contains(ApiConstants.HealthChecks.ReadyTag),
            ResponseWriter = WriteResponseAsync
        });

        return app;
    }

    private static async Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        using var buffer = new MemoryStream();
        await using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("status", report.Status.ToString());
            writer.WriteNumber("totalDurationMs", report.TotalDuration.TotalMilliseconds);

            writer.WriteStartArray("checks");
            foreach (var entry in report.Entries)
            {
                writer.WriteStartObject();
                writer.WriteString("name", entry.Key);
                writer.WriteString("status", entry.Value.Status.ToString());
                writer.WriteNumber("durationMs", entry.Value.Duration.TotalMilliseconds);

                // Exception detail is intentionally omitted: readiness endpoints
                // are frequently exposed and must not leak internals.
                if (!string.IsNullOrWhiteSpace(entry.Value.Description))
                {
                    writer.WriteString("description", entry.Value.Description);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        await context.Response.Body.WriteAsync(buffer.ToArray().AsMemory(), context.RequestAborted);
    }
}
