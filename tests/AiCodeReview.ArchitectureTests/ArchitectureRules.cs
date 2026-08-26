using System.Reflection;
using AiCodeReview.Api;
using AiCodeReview.Domain.Common;
using AiCodeReview.Worker;
using NetArchTest.Rules;
using ArchTestResult = NetArchTest.Rules.TestResult;

namespace AiCodeReview.ArchitectureTests;

/// <summary>
/// Shared vocabulary for the architecture tests. Assemblies are resolved from a
/// marker type so a rename breaks the build rather than silently disabling a rule.
/// </summary>
internal static class ArchitectureRules
{
    internal static class Namespaces
    {
        internal const string Domain = "AiCodeReview.Domain";
        internal const string Application = "AiCodeReview.Application";
        internal const string Infrastructure = "AiCodeReview.Infrastructure";
        internal const string Api = "AiCodeReview.Api";
        internal const string Worker = "AiCodeReview.Worker";
    }

    internal static Assembly DomainAssembly => typeof(Error).Assembly;

    internal static Assembly ApplicationAssembly => typeof(Application.DependencyInjection).Assembly;

    internal static Assembly InfrastructureAssembly => typeof(Infrastructure.DependencyInjection).Assembly;

    internal static Assembly ApiAssembly => typeof(ApiAssemblyMarker).Assembly;

    internal static Assembly WorkerAssembly => typeof(WorkerAssemblyMarker).Assembly;

    internal static string Describe(ArchTestResult result, string rule)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccessful)
        {
            return string.Empty;
        }

        IEnumerable<string> offenders = result.FailingTypeNames ?? [];

        return $"""
            Architecture rule violated: {rule}

            Offending types:
              {string.Join($"{Environment.NewLine}  ", offenders)}
            """;
    }
}
