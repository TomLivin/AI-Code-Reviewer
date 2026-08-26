namespace AiCodeReview.Worker.Configuration;

/// <summary>
/// Worker host settings. Bound from configuration and validated at startup so a
/// bad value fails the deployment rather than surfacing hours later.
/// </summary>
public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    public int HeartbeatSeconds { get; set; } = 60;

    public TimeSpan HeartbeatInterval => TimeSpan.FromSeconds(HeartbeatSeconds);
}
