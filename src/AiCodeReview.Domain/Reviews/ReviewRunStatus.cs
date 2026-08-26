namespace AiCodeReview.Domain.Reviews;

public enum ReviewRunStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
    Cancelled
}
