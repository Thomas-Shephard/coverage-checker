namespace CoverageChecker.Results;

/// <summary>
/// Represents the result of a delta coverage analysis.
/// </summary>
/// <param name="coverage">The filtered coverage information.</param>
/// <param name="hasChangedLines">Whether any changed lines were found in the coverage reports.</param>
/// <param name="gitChangedLineCount">The number of changed lines reported by Git.</param>
/// <param name="matchedCoverageLineCount">The number of changed lines found in the coverage reports.</param>
/// <param name="changedCoverageFileCount">The number of changed files that were present in the coverage reports.</param>
public class DeltaResult(
    Coverage coverage,
    bool hasChangedLines,
    int gitChangedLineCount,
    int matchedCoverageLineCount,
    int changedCoverageFileCount)
{
    /// <summary>
    /// The filtered coverage information containing only the changed lines.
    /// </summary>
    public Coverage Coverage { get; } = coverage;

    /// <summary>
    /// Gets a value indicating whether any of the changed lines were found in the coverage reports.
    /// </summary>
    public bool HasChangedLines { get; } = hasChangedLines;

    /// <summary>
    /// Gets a value indicating whether Git reported any changed lines.
    /// </summary>
    public bool HasGitChangedLines => GitChangedLineCount > 0;

    /// <summary>
    /// Gets the number of changed lines reported by Git.
    /// </summary>
    public int GitChangedLineCount { get; } = gitChangedLineCount;

    /// <summary>
    /// Gets the number of changed lines found in the coverage reports.
    /// </summary>
    public int MatchedCoverageLineCount { get; } = matchedCoverageLineCount;

    /// <summary>
    /// Gets a value indicating whether any changed files were present in the coverage reports.
    /// </summary>
    public bool HasChangedCoverageFiles => ChangedCoverageFileCount > 0;

    /// <summary>
    /// Gets the number of changed files that were present in the coverage reports.
    /// </summary>
    public int ChangedCoverageFileCount { get; } = changedCoverageFileCount;
}
