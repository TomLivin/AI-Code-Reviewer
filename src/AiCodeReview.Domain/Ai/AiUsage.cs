using AiCodeReview.Domain.Common;

namespace AiCodeReview.Domain.Ai;

/// <summary>
/// One model call. Recorded from the very first call rather than added once the
/// bill arrives, so cost per review is measurable throughout.
/// </summary>
public sealed class AiUsage : Entity
{
    private AiUsage()
    {
    }

    public Guid ReviewRunId { get; private set; }

    /// <summary>Which pipeline step made the call, for example <c>FileAnalysis</c>.</summary>
    public string Stage { get; private set; } = null!;

    public string Provider { get; private set; } = null!;

    public string Model { get; private set; } = null!;

    public int InputTokens { get; private set; }

    public int OutputTokens { get; private set; }

    /// <summary>Prompt-cache hits, billed at a reduced rate and the largest real saving.</summary>
    public int CachedInputTokens { get; private set; }

    public decimal EstimatedCostUsd { get; private set; }

    public int DurationMs { get; private set; }

    public bool Succeeded { get; private set; }

    public static AiUsage Record(
        Guid reviewRunId,
        string stage,
        string provider,
        string model,
        int inputTokens,
        int outputTokens,
        int cachedInputTokens,
        decimal estimatedCostUsd,
        int durationMs,
        bool succeeded)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentOutOfRangeException.ThrowIfNegative(inputTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(outputTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(estimatedCostUsd);

        return new AiUsage
        {
            Id = Guid.CreateVersion7(),
            ReviewRunId = reviewRunId,
            Stage = stage,
            Provider = provider,
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            CachedInputTokens = cachedInputTokens,
            EstimatedCostUsd = estimatedCostUsd,
            DurationMs = durationMs,
            Succeeded = succeeded
        };
    }
}
