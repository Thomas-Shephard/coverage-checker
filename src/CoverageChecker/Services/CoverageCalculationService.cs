using CoverageChecker.Results;

namespace CoverageChecker.Services;

public class CoverageCalculationService : ICoverageCalculationService
{
    public double CalculateCoverage(IEnumerable<ICoverageResult> coverageResults, CoverageType coverageType = CoverageType.Line)
    {
        IEnumerable<LineCoverage> lines = coverageResults.SelectMany(coverageResult => coverageResult.GetLines());

        return CalculateCoverage(lines, coverageType);
    }

    private static double CalculateCoverage(IEnumerable<LineCoverage> lines, CoverageType coverageType)
    {
        (Func<LineCoverage, int> coveredFunc, Func<LineCoverage, int> totalFunc) = coverageType switch
        {
            CoverageType.Line   => ((Func<LineCoverage, int>)GetCoveredLines, (Func<LineCoverage, int>)GetLines),
            CoverageType.Branch => (GetCoveredBranches, GetBranches),
            _                   => throw new ArgumentOutOfRangeException(nameof(coverageType), coverageType, "Unknown coverage type")
        };

        int covered = 0, total = 0;

        foreach (LineCoverage line in lines)
        {
            covered += coveredFunc(line);
            total += totalFunc(line);
        }

        return covered / (double)total;

        int GetLines(LineCoverage _) => 1;
        int GetCoveredLines(LineCoverage line) => line.IsCovered ? 1 : 0;
        int GetBranches(LineCoverage line) => line.Branches ?? 0;
        int GetCoveredBranches(LineCoverage line) => line.CoveredBranches ?? 0;
    }
}