namespace AiCodeReview.Infrastructure.Persistence;

/// <summary>
/// Column sizes in one place. Unbounded text columns are fine in PostgreSQL,
/// but a declared length documents intent and stops a malformed upstream value
/// from being stored at unbounded size.
/// </summary>
internal static class ColumnLengths
{
    internal const int Enum = 32;
    internal const int CommitSha = 40;
    internal const int GitHubLogin = 100;
    internal const int RepositoryFullName = 256;
    internal const int GitRef = 255;
    internal const int FilePath = 1024;
    internal const int Title = 300;
    internal const int Url = 2048;
    internal const int Email = 320;
    internal const int RuleCode = 32;
    internal const int Fingerprint = 64;
    internal const int ErrorCode = 100;
    internal const int ErrorMessage = 2000;
    internal const int WorkerId = 128;
    internal const int PipelineStage = 64;
    internal const int ProviderName = 64;
    internal const int ModelName = 128;
    internal const int SuppressionReason = 500;
}
