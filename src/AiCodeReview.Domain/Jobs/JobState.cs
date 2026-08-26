namespace AiCodeReview.Domain.Jobs;

public enum JobState
{
    Pending,
    Running,
    Succeeded,
    Failed,
    DeadLettered
}
