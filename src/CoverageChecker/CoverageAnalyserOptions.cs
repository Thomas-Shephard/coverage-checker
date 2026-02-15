namespace CoverageChecker;

/// <summary>
/// Options for the <see cref="CoverageAnalyser"/>.
/// </summary>
public record CoverageAnalyserOptions
{
    /// <summary>
    /// The format of the coverage file.
    /// </summary>
    public CoverageFormat CoverageFormat { get; init; } = CoverageFormat.Auto;

    /// <summary>
    /// The directory to search for coverage files within.
    /// </summary>
    public string Directory { get; init; } = Environment.CurrentDirectory;

    /// <summary>
    /// The glob patterns to use to search for coverage files.
    /// </summary>
    public IEnumerable<string> GlobPatterns { get; init; } = ["**/*.xml"];

    /// <summary>
    /// Glob patterns of files to include in the coverage analysis.
    /// </summary>
    public IEnumerable<string>? Include { get; init; }

    /// <summary>
    /// Glob patterns of files to exclude from the coverage analysis.
    /// </summary>
    public IEnumerable<string>? Exclude { get; init; }
}
