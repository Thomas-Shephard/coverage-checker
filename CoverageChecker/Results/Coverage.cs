using CoverageChecker.Utils;

namespace CoverageChecker.Results;

public class Coverage(FileCoverage[] files) {
    public FileCoverage[] Files { get; } = files;

    public bool TryGetFile(string path, out FileCoverage? file) {
        file = Files.FirstOrDefault(file => file.Path == path);
        return file is not null;
    }

    public double CalculateOverallCoverage(CoverageType coverageType = CoverageType.Line) {
        return Files.CalculateCoverage(coverageType);
    }

    public double CalculatePackageCoverage(string packageName, CoverageType coverageType = CoverageType.Line) {
        FileCoverage[] filteredFiles = Files.Where(file => file.PackageName == packageName)
                                            .ToArray();

        if (filteredFiles.Length is 0)
            throw new CoverageCalculationException("No files found for the specified package name");

        return filteredFiles.CalculateCoverage(coverageType);
    }
}