namespace CoverageChecker.Results;

/// <summary>
/// Represents the result of a delta coverage analysis.
/// </summary>
/// <param name="coverage">The filtered coverage information.</param>
/// <param name="hasChangedLines">Whether any changed lines were found in the coverage reports.</param>
public class DeltaResult(Coverage coverage, bool hasChangedLines)
{
    /// <summary>
    /// The filtered coverage information containing only the changed lines.
    /// </summary>
    public Coverage Coverage { get; } = coverage;

    /// <summary>
    /// Gets a value indicating whether any of the changed lines were found in the coverage reports.
    /// </summary>
    public bool HasChangedLines { get; } = hasChangedLines;
}
