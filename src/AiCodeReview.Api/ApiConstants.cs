namespace AiCodeReview.Api;

public static class ApiConstants
{
    public const string ApplicationName = "AiCodeReview.Api";

    public static class Headers
    {
        public const string CorrelationId = "X-Correlation-Id";
    }

    public static class Logging
    {
        public const string CorrelationIdProperty = "CorrelationId";
    }

    public static class HealthChecks
    {
        /// <summary>Checks that must pass before the instance may receive traffic.</summary>
        public const string ReadyTag = "ready";

        public const string LivePath = "/health";
        public const string ReadyPath = "/health/ready";
    }

    /// <summary>
    /// ProblemDetails <c>type</c> values. URNs are used rather than https URLs
    /// so the contract does not depend on a documentation site being reachable.
    /// </summary>
    public static class ProblemTypes
    {
        private const string Prefix = "urn:aicodereview:problem:";

        public const string Validation = Prefix + "validation";
        public const string NotFound = Prefix + "not-found";
        public const string Conflict = Prefix + "conflict";
        public const string Forbidden = Prefix + "forbidden";
        public const string Unauthorized = Prefix + "unauthorized";
        public const string External = Prefix + "external-dependency";
        public const string Unexpected = Prefix + "unexpected";
    }
}
