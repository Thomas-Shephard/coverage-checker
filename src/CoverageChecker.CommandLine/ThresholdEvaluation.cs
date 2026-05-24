namespace CoverageChecker.CommandLine;

internal sealed record ThresholdEvaluation(
    bool HasNoApplicableLineCoverage,
    bool IsLineCoverageBelowThreshold,
    bool IsBranchCoverageBelowThreshold,
    bool HasNoApplicableDeltaLineCoverage,
    bool IsDeltaLineCoverageBelowThreshold,
    bool IsDeltaBranchCoverageBelowThreshold,
    bool HasChangedCoverageFilesWithoutDeltaLines,
    bool HasDeltaChangedLinesWithoutCoverageResults,
    bool HasStrictDeltaMissingFiles)
{
    public bool Failed =>
        HasNoApplicableLineCoverage ||
        IsLineCoverageBelowThreshold ||
        IsBranchCoverageBelowThreshold ||
        HasNoApplicableDeltaLineCoverage ||
        IsDeltaLineCoverageBelowThreshold ||
        IsDeltaBranchCoverageBelowThreshold ||
        HasChangedCoverageFilesWithoutDeltaLines ||
        HasDeltaChangedLinesWithoutCoverageResults ||
        HasStrictDeltaMissingFiles;
}

internal static class ThresholdEvaluator
{
    public static ThresholdEvaluation Evaluate(CoverageResult result, CommandLineOptions options)
    {
        bool hasDeltaCoverageResults = result is { HasDeltaChangedLines: true, DeltaCoverage: not null };
        bool hasDeltaChangedLinesWithoutCoverageResults = result is { HasDeltaChangedLines: true, DeltaCoverage: null };
        bool shouldEvaluateDeltaThresholds = options.Delta && hasDeltaCoverageResults;

        bool hasNoApplicableDeltaLineCoverage = shouldEvaluateDeltaThresholds && double.IsNaN(result.DeltaLineCoverage);

        return new ThresholdEvaluation(
            HasNoApplicableLineCoverage: double.IsNaN(result.LineCoverage),
            IsLineCoverageBelowThreshold: !double.IsNaN(result.LineCoverage) && options.LineThreshold > result.LineCoverage,
            IsBranchCoverageBelowThreshold: !double.IsNaN(result.BranchCoverage) && options.BranchThreshold > result.BranchCoverage,
            HasNoApplicableDeltaLineCoverage: hasNoApplicableDeltaLineCoverage,
            IsDeltaLineCoverageBelowThreshold: shouldEvaluateDeltaThresholds && !double.IsNaN(result.DeltaLineCoverage) && options.EffectiveDeltaLineThreshold > result.DeltaLineCoverage,
            IsDeltaBranchCoverageBelowThreshold: shouldEvaluateDeltaThresholds && !double.IsNaN(result.DeltaBranchCoverage) && options.EffectiveDeltaBranchThreshold > result.DeltaBranchCoverage,
            HasChangedCoverageFilesWithoutDeltaLines: options.Delta && result.HasChangedCoverageFiles && !hasDeltaCoverageResults,
            HasDeltaChangedLinesWithoutCoverageResults: options.Delta && hasDeltaChangedLinesWithoutCoverageResults,
            HasStrictDeltaMissingFiles: options is { Delta: true, StrictDelta: true } && result.ChangedFilesMissingCoverage.Count > 0);
    }
}
