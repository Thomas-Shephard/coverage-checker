using CoverageChecker.Utils;

namespace CoverageChecker.Results;

/// <summary>
/// Represents coverage information for a collection of files.
/// </summary>
public class Coverage {
    /// <summary>
    /// The files that coverage information has been obtained for.
    /// </summary>
    public IReadOnlyList<FileCoverage> Files => _files.AsReadOnly();
    private readonly List<FileCoverage> _files = [];

    internal Coverage() {

    }

    internal Coverage(IEnumerable<FileCoverage> files) : this() {
        _files = files.ToList();
    }

    internal FileCoverage GetOrCreateFile(string filePath, string? packageName = null) {
        FileCoverage? file = _files.Find(file => file.Path == filePath && file.PackageName == packageName);

        if (file is not null) return file;

        file = new FileCoverage(filePath, packageName);
        _files.Add(file);

        return file;
    }

    /// <summary>
    /// Calculates the coverage for all files.
    /// </summary>
    /// <param name="coverageType">The type of coverage to calculate. Defaults to <see cref="CoverageType.Line"/>.</param>
    /// <returns>The coverage for all files.</returns>
    public double CalculateOverallCoverage(CoverageType coverageType = CoverageType.Line) {
        return _files.CalculateCoverage(coverageType);
    }

    /// <summary>
    /// Calculates the coverage for all files that are part of the specified package.
    /// </summary>
    /// <param name="packageName">The name of the package to filter by.</param>
    /// <param name="coverageType">The type of coverage to calculate. Defaults to <see cref="CoverageType.Line"/>.</param>
    /// <returns>The coverage for all files that are part of the specified package.</returns>
    /// <exception cref="CoverageCalculationException">Thrown when no files are found that are part of the specified package.</exception>
    public double CalculatePackageCoverage(string packageName, CoverageType coverageType = CoverageType.Line) {
        FileCoverage[] filteredFiles = _files.Where(file => file.PackageName == packageName)
                                             .ToArray();

        if (filteredFiles.Length is 0)
            throw new CoverageCalculationException("No files found for the specified package name");

        return filteredFiles.CalculateCoverage(coverageType);
    }
}