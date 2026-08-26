namespace AiCodeReview.Domain.Reviews;

/// <summary>
/// How sure the source is. Deterministic analysers always report
/// <see cref="High"/>; the model reports its own confidence, which the risk
/// score discounts accordingly.
/// </summary>
public enum FindingConfidence
{
    Low,
    Medium,
    High
}
