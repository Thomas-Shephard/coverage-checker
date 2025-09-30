namespace CoverageChecker.Results;

/// <summary>
/// Represents any type of coverage result.
/// </summary>
public interface ICoverageResult
{
    /// <summary>
    /// All lines of code covered by this result.
    /// </summary>
    IReadOnlyList<LineCoverage> Lines { get; }
}