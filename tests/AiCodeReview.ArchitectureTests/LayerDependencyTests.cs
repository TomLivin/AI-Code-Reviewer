using NetArchTest.Rules;
using ArchTestResult = NetArchTest.Rules.TestResult;

namespace AiCodeReview.ArchitectureTests;

/// <summary>
/// Enforces the dependency direction recorded in ADR-001. These run in CI, so
/// the layering cannot quietly rot between milestones.
/// </summary>
public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_should_not_depend_on_any_other_layer()
    {
        ArchTestResult result = Types.InAssembly(ArchitectureRules.DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ArchitectureRules.Namespaces.Application,
                ArchitectureRules.Namespaces.Infrastructure,
                ArchitectureRules.Namespaces.Api,
                ArchitectureRules.Namespaces.Worker)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            ArchitectureRules.Describe(result, "Domain must not reference any outer layer."));
    }

    [Fact]
    public void Domain_should_not_depend_on_infrastructure_concerns()
    {
        ArchTestResult result = Types.InAssembly(ArchitectureRules.DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.Extensions",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "System.Net.Http",
                "Npgsql")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(ArchitectureRules.Describe(
            result,
            "Domain must stay free of frameworks, transports and persistence."));
    }

    [Fact]
    public void Application_should_not_depend_on_infrastructure_or_hosts()
    {
        ArchTestResult result = Types.InAssembly(ArchitectureRules.ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ArchitectureRules.Namespaces.Infrastructure,
                ArchitectureRules.Namespaces.Api,
                ArchitectureRules.Namespaces.Worker)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(ArchitectureRules.Describe(
            result,
            "Application defines abstractions; Infrastructure implements them, never the reverse."));
    }

    [Fact]
    public void Infrastructure_should_not_depend_on_hosts()
    {
        ArchTestResult result = Types.InAssembly(ArchitectureRules.InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ArchitectureRules.Namespaces.Api,
                ArchitectureRules.Namespaces.Worker)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(ArchitectureRules.Describe(
            result,
            "Infrastructure must not reach back into a host."));
    }

    [Fact]
    public void Api_and_Worker_should_not_depend_on_each_other()
    {
        ArchTestResult apiResult = Types.InAssembly(ArchitectureRules.ApiAssembly)
            .ShouldNot()
            .HaveDependencyOn(ArchitectureRules.Namespaces.Worker)
            .GetResult();

        apiResult.IsSuccessful.ShouldBeTrue(ArchitectureRules.Describe(
            apiResult,
            "The API must not depend on the Worker."));

        ArchTestResult workerResult = Types.InAssembly(ArchitectureRules.WorkerAssembly)
            .ShouldNot()
            .HaveDependencyOn(ArchitectureRules.Namespaces.Api)
            .GetResult();

        workerResult.IsSuccessful.ShouldBeTrue(ArchitectureRules.Describe(
            workerResult,
            "The Worker must not depend on the API. They share Application, not each other."));
    }
}
