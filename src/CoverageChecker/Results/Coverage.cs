using CoverageChecker.Utils;

namespace CoverageChecker.Results;

public class Coverage {
    public IReadOnlyList<FileCoverage> Files => _files.AsReadOnly();
    private readonly List<FileCoverage> _files = [];

    internal Coverage() {

    }

    internal Coverage(IEnumerable<FileCoverage> files) : this() {
        _files = files.ToList();
    }

    public FileCoverage? GetFile(string path, string? packageName = null) {
        return Files.FirstOrDefault(file => file.Path == path && file.PackageName == packageName);
    }

    internal FileCoverage GetOrCreateFile(string filePath, string? packageName = null) {
        FileCoverage? file = GetFile(filePath, packageName);

        if (file is not null) return file;

        file = new FileCoverage(filePath, packageName);
        _files.Add(file);

        return file;
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