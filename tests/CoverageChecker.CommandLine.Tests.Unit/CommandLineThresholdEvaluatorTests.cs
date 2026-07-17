using CoverageChecker.CommandLine;
using CoverageChecker.Results;
using System.Runtime.CompilerServices;

namespace CoverageChecker.CommandLine.Tests.Unit;

public class CommandLineThresholdEvaluatorTests
{
    public static IEnumerable<TestCaseData> FailureCases()
    {
        yield return new TestCaseData(
            Result(lineCoverage: double.NaN),
            Options(),
            nameof(ThresholdEvaluation.HasNoApplicableLineCoverage))
            .SetName("Overall line coverage NaN fails");

        yield return new TestCaseData(
            Result(lineCoverage: 0.79),
            Options(lineThreshold: 80),
            nameof(ThresholdEvaluation.IsLineCoverageBelowThreshold))
            .SetName("Overall line coverage below threshold fails");

        yield return new TestCaseData(
            Result(branchCoverage: 0.79),
            Options(branchThreshold: 80),
            nameof(ThresholdEvaluation.IsBranchCoverageBelowThreshold))
            .SetName("Overall branch coverage below threshold fails");

        yield return new TestCaseData(
            Result(deltaCoverage: CreateCoverage(), deltaLineCoverage: double.NaN, hasDeltaChangedLines: true),
            Options(delta: true),
            nameof(ThresholdEvaluation.HasNoApplicableDeltaLineCoverage))
            .SetName("Delta line coverage NaN fails when changed lines and delta coverage results exist");

        yield return new TestCaseData(
            Result(deltaCoverage: CreateCoverage(), deltaLineCoverage: 0.79, hasDeltaChangedLines: true),
            Options(lineThreshold: 80, delta: true),
            nameof(ThresholdEvaluation.IsDeltaLineCoverageBelowThreshold))
            .SetName("Delta line coverage below effective threshold fails");

        yield return new TestCaseData(
            Result(deltaCoverage: CreateCoverage(), deltaBranchCoverage: 0.79, hasDeltaChangedLines: true),
            Options(branchThreshold: 80, delta: true),
            nameof(ThresholdEvaluation.IsDeltaBranchCoverageBelowThreshold))
            .SetName("Delta branch coverage below effective threshold fails");

        yield return new TestCaseData(
            Result(hasChangedCoverageFiles: true),
            Options(delta: true),
            nameof(ThresholdEvaluation.HasChangedCoverageFilesWithoutDeltaLines))
            .SetName("Changed coverage files with no matched delta lines fail when delta is true");

        yield return new TestCaseData(
            Result(hasDeltaChangedLines: true, changedFilesMissingCoverage: ["missing.cs"]),
            Options(delta: true),
            nameof(ThresholdEvaluation.HasDeltaChangedLinesWithoutCoverageResults))
            .SetName("Delta changed lines without coverage results fail when delta is true");

        yield return new TestCaseData(
            Result(changedFilesMissingCoverage: ["missing.cs"]),
            Options(delta: true, strictDelta: true),
            nameof(ThresholdEvaluation.HasStrictDeltaMissingFiles))
            .SetName("Strict delta missing files fail when delta and strict delta are true");
    }

    public static IEnumerable<TestCaseData> PassingCases()
    {
        yield return new TestCaseData(
            Result(branchCoverage: double.NaN),
            Options())
            .SetName("Overall branch coverage NaN does not fail");

        yield return new TestCaseData(
            Result(deltaCoverage: CreateCoverage(), deltaLineCoverage: 0, deltaBranchCoverage: 0, hasDeltaChangedLines: true),
            Options(deltaLineThreshold: 100, deltaBranchThreshold: 100))
            .SetName("Delta thresholds are ignored when delta is false");

        yield return new TestCaseData(
            Result(deltaCoverage: CreateCoverage(), deltaBranchCoverage: double.NaN, hasDeltaChangedLines: true),
            Options(delta: true))
            .SetName("Delta branch coverage NaN does not fail");

        yield return new TestCaseData(
            Result(changedFilesMissingCoverage: ["missing.cs"]),
            Options(delta: false, strictDelta: true))
            .SetName("Strict delta missing files do not fail when delta is false");

        yield return new TestCaseData(
            Result(changedFilesMissingCoverage: ["missing.cs"]),
            Options(delta: true, strictDelta: false))
            .SetName("Strict delta missing files do not fail when strict delta is false");

        yield return new TestCaseData(Result(), Options()).SetName("Passing result does not fail");
    }

    [TestCaseSource(nameof(FailureCases))]
    public void EvaluateFailureCasesReturnsExpectedFailure(object result, object options, string expectedFailureProperty)
    {
        ThresholdEvaluation evaluation = ThresholdEvaluator.Evaluate((CoverageResult)result, (CommandLineOptions)options);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(evaluation.Failed, Is.True);
            Assert.That(GetFailureProperty(evaluation, expectedFailureProperty), Is.True);
        }
    }

    [TestCaseSource(nameof(PassingCases))]
    public void EvaluatePassingCasesDoesNotFail(object result, object options)
    {
        ThresholdEvaluation evaluation = ThresholdEvaluator.Evaluate((CoverageResult)result, (CommandLineOptions)options);

        Assert.That(evaluation.Failed, Is.False);
    }

    private static bool GetFailureProperty(ThresholdEvaluation evaluation, string propertyName)
    {
        return (bool)typeof(ThresholdEvaluation).GetProperty(propertyName)!.GetValue(evaluation)!;
    }

    private static CoverageResult Result(
        double lineCoverage = 0.9,
        double branchCoverage = 0.9,
        Coverage? deltaCoverage = null,
        double deltaLineCoverage = 0.9,
        double deltaBranchCoverage = 0.9,
        bool hasDeltaChangedLines = false,
        bool hasChangedCoverageFiles = false,
        IReadOnlyList<string>? changedFilesMissingCoverage = null)
    {
        return new CoverageResult(
            OverallCoverage: CreateCoverage(),
            LineCoverage: lineCoverage,
            BranchCoverage: branchCoverage,
            DeltaCoverage: deltaCoverage,
            DeltaLineCoverage: deltaLineCoverage,
            DeltaBranchCoverage: deltaBranchCoverage,
            HasDeltaChangedLines: hasDeltaChangedLines,
            HasGitDeltaChangedLines: hasDeltaChangedLines,
            HasChangedCoverageFiles: hasChangedCoverageFiles,
            ChangedFilesMissingCoverage: changedFilesMissingCoverage ?? []);
    }

    private static CheckOptions Options(
        double lineThreshold = 80,
        double branchThreshold = 80,
        bool delta = false,
        bool strictDelta = false,
        double? deltaLineThreshold = null,
        double? deltaBranchThreshold = null)
    {
        return new CheckOptions
        {
            LineThreshold = lineThreshold,
            BranchThreshold = branchThreshold,
            Delta = delta,
            StrictDelta = strictDelta,
            DeltaLineThreshold = deltaLineThreshold,
            DeltaBranchThreshold = deltaBranchThreshold
        };
    }

    private static Coverage CreateCoverage()
    {
        return (Coverage)RuntimeHelpers.GetUninitializedObject(typeof(Coverage));
    }
}
