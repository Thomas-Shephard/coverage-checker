using CoverageChecker.Results;

namespace CoverageChecker.CommandLine;

internal record CoverageResult(
    Coverage OverallCoverage,
    double LineCoverage,
    double BranchCoverage,
    Coverage? DeltaCoverage,
    double DeltaLineCoverage,
    double DeltaBranchCoverage,
    bool HasDeltaChangedLines);
