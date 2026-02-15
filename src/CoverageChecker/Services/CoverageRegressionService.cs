using System.Diagnostics.CodeAnalysis;
using CoverageChecker.Results;

namespace CoverageChecker.Services;

internal class CoverageRegressionService : ICoverageRegressionService
{
    private static readonly CoverageType[] CoverageTypes = Enum.GetValues<CoverageType>();

    public RegressionResult CheckRegression(Coverage baseline, Coverage current, double epsilon = CoverageAnalyser.DefaultEpsilon)
    {
        Dictionary<(string Path, string? PackageName), FileCoverage> currentFilesMap = current.Files.ToDictionary(f => (f.Path, f.PackageName));

        List<RegressedFile> regressedFiles = baseline.Files
                                                     .SelectMany(baselineFile =>
                                                     {
                                                         currentFilesMap.TryGetValue((baselineFile.Path, baselineFile.PackageName), out FileCoverage? currentFile);
                                                         return CheckFileRegression(baselineFile, currentFile, epsilon);
                                                     })
                                                     .ToList();

        return new RegressionResult(regressedFiles);
    }

    private static IEnumerable<RegressedFile> CheckFileRegression(FileCoverage baselineFile, FileCoverage? currentFile, double epsilon)
    {
        foreach (CoverageType type in CoverageTypes)
        {
            if (TryGetRegression(baselineFile, currentFile, type, epsilon, out RegressedFile? regression))
            {
                yield return regression;
            }
        }
    }

    private static bool TryGetRegression(FileCoverage baselineFile, FileCoverage? currentFile, CoverageType type, double epsilon, [NotNullWhen(true)] out RegressedFile? regression)
    {
        regression = null;
        double baselineCoverage = baselineFile.CalculateFileCoverage(type);
        if (double.IsNaN(baselineCoverage))
        {
            return false;
        }

        double currentCoverage = currentFile?.CalculateFileCoverage(type) ?? 0.0;
        if (double.IsNaN(currentCoverage))
        {
            return false;
        }

        if (currentCoverage >= baselineCoverage - epsilon)
        {
            return false;
        }

        regression = new RegressedFile(
                                       baselineFile.Path,
                                       baselineFile.PackageName,
                                       baselineCoverage,
                                       currentCoverage,
                                       currentCoverage - baselineCoverage,
                                       type
                                      );
        return true;
    }
}
