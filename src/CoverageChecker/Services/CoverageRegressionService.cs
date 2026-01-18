using CoverageChecker.Results;

namespace CoverageChecker.Services;

internal class CoverageRegressionService : ICoverageRegressionService
{
    private static readonly CoverageType[] CoverageTypes = Enum.GetValues<CoverageType>();

    public RegressionResult CheckRegression(Coverage baseline, Coverage current, double epsilon = CoverageAnalyser.DefaultEpsilon)
    {
        List<RegressedFile> regressedFiles = [];
        
        Dictionary<(string Path, string? PackageName), FileCoverage> currentFilesMap = new();
        foreach (FileCoverage file in current.Files)
        {
            currentFilesMap.TryAdd((file.Path, file.PackageName), file);
        }

        foreach (FileCoverage baselineFile in baseline.Files)
        {
            foreach (CoverageType coverageType in CoverageTypes)
            {
                double baselineCoverage = baselineFile.CalculateFileCoverage(coverageType);

                if (double.IsNaN(baselineCoverage))
                {
                    continue;
                }

                if (currentFilesMap.TryGetValue((baselineFile.Path, baselineFile.PackageName), out FileCoverage? currentFile))
                {
                    double newCoverage = currentFile.CalculateFileCoverage(coverageType);

                    if (double.IsNaN(newCoverage))
                    {
                        continue;
                    }

                    if (newCoverage < baselineCoverage - epsilon)
                    {
                        regressedFiles.Add(new RegressedFile(
                            baselineFile.Path,
                            baselineFile.PackageName,
                            baselineCoverage,
                            newCoverage,
                            newCoverage - baselineCoverage,
                            coverageType
                        ));
                    }
                }
                else
                {
                    // If missing in current, it is treated as a regression to 0%
                    const double effectiveNewCoverage = 0.0;

                    if (effectiveNewCoverage < baselineCoverage - epsilon)
                    {
                        regressedFiles.Add(new RegressedFile(
                            baselineFile.Path,
                            baselineFile.PackageName,
                            baselineCoverage,
                            effectiveNewCoverage,
                            effectiveNewCoverage - baselineCoverage,
                            coverageType
                        ));
                    }
                }
            }
        }

        return new RegressionResult(regressedFiles);
    }
}
