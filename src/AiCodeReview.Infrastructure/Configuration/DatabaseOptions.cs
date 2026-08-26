namespace AiCodeReview.Infrastructure.Configuration;

/// <summary>
/// Database tuning. The connection string itself is read from the standard
/// <c>ConnectionStrings</c> section instead of here, so a platform can supply it
/// through the conventional <c>ConnectionStrings__AppDb</c> environment
/// variable without knowing anything about this type.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public const string ConnectionStringName = "AppDb";

    /// <summary>Transient fault retries. Managed PostgreSQL drops connections during failover.</summary>
    public int MaxRetryCount { get; set; } = 3;

    public int MaxRetryDelaySeconds { get; set; } = 10;

    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Logs parameter values. Development only: parameters routinely contain
    /// access tokens and repository content.
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; }

    public bool EnableDetailedErrors { get; set; }
}
