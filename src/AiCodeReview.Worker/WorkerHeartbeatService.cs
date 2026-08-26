using AiCodeReview.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace AiCodeReview.Worker;

/// <summary>
/// Emits a periodic liveness log so an idle Worker is distinguishable from a
/// hung one. Replaced by the job dispatcher in M4, which takes over this loop.
/// </summary>
public sealed class WorkerHeartbeatService(
    IOptions<WorkerOptions> options,
    TimeProvider timeProvider,
    ILogger<WorkerHeartbeatService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = options.Value.HeartbeatInterval;

        logger.WorkerStarted(WorkerConstants.ApplicationName, interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval, timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                logger.Heartbeat();
            }
        }
        catch (OperationCanceledException)
        {
            logger.WorkerStopping(WorkerConstants.ApplicationName);
        }
    }
}
