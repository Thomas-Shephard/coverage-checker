using CoverageChecker.Results;

namespace CoverageChecker.Services;

/// <summary>
/// Service for filtering coverage information based on changed lines.
/// </summary>
internal interface IDeltaCoverageService
{
    /// <summary>
    /// Filters the coverage information to only include lines that have been changed.
    /// </summary>
    /// <param name="coverage">The coverage information to filter.</param>
    /// <param name="changedLines">A dictionary where the key is the file path and the value is a set of changed line numbers.</param>
    /// <returns>A <see cref="DeltaResult"/> object containing the filtered coverage information and status.</returns>
    DeltaResult FilterCoverage(Coverage coverage, IDictionary<string, HashSet<int>> changedLines);
}
