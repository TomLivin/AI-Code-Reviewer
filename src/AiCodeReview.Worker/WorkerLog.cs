namespace AiCodeReview.Worker;

/// <summary>
/// Source-generated log methods. The generator emits a cached, strongly typed
/// delegate per message, so arguments are neither boxed into a params array nor
/// evaluated at all when the level is disabled (CA1873). Used for the Worker's
/// hot paths; plain <c>ILogger</c> calls remain fine for one-off Error logs.
/// </summary>
internal static partial class WorkerLog
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "{Application} started. Heartbeat interval {IntervalSeconds}s.")]
    internal static partial void WorkerStarted(this ILogger logger, string application, double intervalSeconds);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Debug,
        Message = "Heartbeat.")]
    internal static partial void Heartbeat(this ILogger logger);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "{Application} shutting down.")]
    internal static partial void WorkerStopping(this ILogger logger, string application);
}
