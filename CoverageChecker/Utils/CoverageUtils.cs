using CoverageChecker.Results;

namespace CoverageChecker.Utils;

internal static class CoverageUtils {
    internal static double CalculateCoverage(this FileCoverage[] files, CoverageType coverageType) {
        LineCoverage[] lines = files.SelectMany(file => file.Lines)
                                    .ToArray();

        return lines.CalculateCoverage(coverageType);
    }

    internal static double CalculateCoverage(this LineCoverage[] lines, CoverageType coverageType) {
        int total, covered;

        switch (coverageType) {
            case CoverageType.Line:
                total = lines.Length;
                covered = lines.Count(line => line.IsCovered);
                break;
            case CoverageType.Branch:
                total = lines.Sum(line => line.Branches ?? 0);
                covered = lines.Sum(line => line.CoveredBranches ?? 0);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(coverageType), coverageType, "Unknown coverage type");
        }

        return (double)covered / total;
    }
}