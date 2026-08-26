namespace AiCodeReview.Domain.Reviews;

/// <summary>
/// Where a finding came from. Kept separate rather than merged, because a
/// finding both a deterministic analyser and the model reported independently
/// is the strongest signal available and is scored accordingly.
/// </summary>
public enum FindingSource
{
    StaticAnalyzer,
    Ai,
    Corroborated
}
