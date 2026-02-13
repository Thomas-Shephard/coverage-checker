using CoverageChecker.Results;

namespace CoverageChecker.CommandLine;

internal sealed record CoverageResult(
    Coverage OverallCoverage,
    double LineCoverage,
    double BranchCoverage,
    Coverage? DeltaCoverage,
    double DeltaLineCoverage,
    double DeltaBranchCoverage,
    bool HasDeltaChangedLines);
