using CoverageChecker.Results;

namespace CoverageChecker.Utils;

internal static class CoverageCalculationUtils
{
    internal static double CalculateCoverage(this IEnumerable<FileCoverage> files, CoverageType coverageType)
    {
        return files.SelectMany(file => file.Lines)
                    .CalculateCoverage(coverageType);
    }

    internal static double CalculateCoverage(this IEnumerable<LineCoverage> lines, CoverageType coverageType)
    {
        (int covered, int total) = coverageType switch
        {
            CoverageType.Line   => SumCoveredAndTotal(lines, GetCoveredLines, GetLines),
            CoverageType.Branch => SumCoveredAndTotal(lines, GetCoveredBranches, GetBranches),
            _                   => throw new ArgumentOutOfRangeException(nameof(coverageType), coverageType, "Unknown coverage type")
        };

        return (double)covered / total;

        int GetLines(LineCoverage _) => 1;
        int GetCoveredLines(LineCoverage line) => line.IsCovered ? 1 : 0;
        int GetBranches(LineCoverage line) => line.Branches ?? 0;
        int GetCoveredBranches(LineCoverage line) => line.CoveredBranches ?? 0;
    }

    private static (int covered, int total) SumCoveredAndTotal(IEnumerable<LineCoverage> lines, Func<LineCoverage, int> coveredSelector, Func<LineCoverage, int> totalSelector)
    {
        int covered = 0, total = 0;

        foreach (LineCoverage line in lines)
        {
            covered += coveredSelector(line);
            total += totalSelector(line);
        }

        return (covered, total);
    }
}