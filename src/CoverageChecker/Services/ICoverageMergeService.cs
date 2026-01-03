using CoverageChecker.Results;

namespace CoverageChecker.Services;

internal interface ICoverageMergeService
{
    /// <summary>
    /// Merges two line coverage objects.
    /// </summary>
    /// <param name="existing">The existing line coverage.</param>
    /// <param name="incoming">The incoming line coverage.</param>
    void Merge(LineCoverage existing, LineCoverage incoming);
}