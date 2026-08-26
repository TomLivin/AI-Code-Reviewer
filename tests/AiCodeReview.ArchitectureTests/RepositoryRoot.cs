namespace AiCodeReview.ArchitectureTests;

internal static class RepositoryRoot
{
    private const string SolutionFileName = "AiCodeReview.slnx";

    /// <summary>
    /// Walks up from the test output directory until the solution file is found,
    /// so the tests work from the CLI, an IDE and CI without configuration.
    /// </summary>
    internal static string Locate()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate {SolutionFileName} above {AppContext.BaseDirectory}.");
    }
}
