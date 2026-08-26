namespace AiCodeReview.ArchitectureTests;

/// <summary>
/// Assembly-level rules cannot express "only the composition root may know about
/// Infrastructure", because a host legitimately references it. This inspects the
/// source instead: outside Program.cs, a host must talk to abstractions only.
/// </summary>
public sealed class CompositionRootTests
{
    private const string CompositionRootFile = "Program.cs";
    private const string ForbiddenUsing = "using AiCodeReview.Infrastructure";

    [Theory]
    [InlineData("AiCodeReview.Api")]
    [InlineData("AiCodeReview.Worker")]
    public void Host_may_reference_infrastructure_only_from_its_composition_root(string projectName)
    {
        string projectDirectory = Path.Combine(RepositoryRoot.Locate(), "src", projectName);

        Directory.Exists(projectDirectory).ShouldBeTrue($"Expected to find the project at {projectDirectory}.");

        var offenders = EnumerateSourceFiles(projectDirectory)
            .Where(file => !string.Equals(Path.GetFileName(file), CompositionRootFile, StringComparison.Ordinal))
            .Where(file => File.ReadAllText(file).Contains(ForbiddenUsing, StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(projectDirectory, file))
            .ToList();

        offenders.ShouldBeEmpty(
            $"""
            {projectName} may only reference Infrastructure from {CompositionRootFile}.
            Everything else must depend on Application abstractions.

            Offending files:
              {string.Join($"{Environment.NewLine}  ", offenders)}
            """);
    }

    private static IEnumerable<string> EnumerateSourceFiles(string directory) =>
        Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(static file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(static file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
}
