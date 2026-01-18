namespace CoverageChecker.Results;

/// <summary>
/// Represents a file that has regressed in coverage.
/// </summary>
/// <param name="Path">The path to the file.</param>
/// <param name="PackageName">The name of the package the file belongs to.</param>
/// <param name="BaselineCoverage">The coverage percentage in the baseline.</param>
/// <param name="NewCoverage">The coverage percentage in the current run.</param>
/// <param name="CoverageDiff">The difference between the new and baseline coverage.</param>
/// <param name="CoverageType">The type of coverage that regressed.</param>
public record RegressedFile(
    string Path,
    string? PackageName,
    double BaselineCoverage,
    double NewCoverage,
    double CoverageDiff,
    CoverageType CoverageType
);
