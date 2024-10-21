using CoverageChecker.Utils;

namespace CoverageChecker.Results;

public class FileCoverage {
    public string Path { get; }
    public string? PackageName { get; }
    public IReadOnlyList<LineCoverage> Lines => _lines.AsReadOnly();
    private readonly List<LineCoverage> _lines = [];

    internal FileCoverage(string path, string? packageName = null) {
        Path = path;
        PackageName = packageName;
    }

    internal FileCoverage(IEnumerable<LineCoverage> lines, string path, string? packageName = null) : this(path, packageName) {
        _lines = lines.ToList();
    }

    internal void AddLine(int lineNumber, bool isCovered, int? branches = null, int? coveredBranches = null, string? className = null, string? methodName = null, string? methodSignature = null) {
        LineCoverage? existingLine = Lines.FirstOrDefault(line => line.LineNumber == lineNumber);
        LineCoverage newLine = new(lineNumber, isCovered, branches, coveredBranches, className, methodName, methodSignature);

        if (existingLine is not null) {
            existingLine.MergeWith(newLine);
        } else {
            _lines.Add(newLine);
        }
    }

    public double CalculateFileCoverage(CoverageType coverageType = CoverageType.Line) {
        return Lines.CalculateCoverage(coverageType);
    }

    public double CalculateClassCoverage(string className, CoverageType coverageType = CoverageType.Line) {
        LineCoverage[] filteredLines = Lines.Where(line => line.ClassName == className)
                                            .ToArray();

        if (filteredLines.Length is 0)
            throw new CoverageCalculationException("No lines found for the specified class name");

        return filteredLines.CalculateCoverage(coverageType);
    }

    public double CalculateMethodCoverage(string methodName, string? methodSignature = null, CoverageType coverageType = CoverageType.Line) {
        LineCoverage[] filteredLines = Lines.Where(line => {
                                                // If the method signature is null, only the method name is checked
                                                if (methodSignature is null)
                                                    return line.MethodName == methodName;
                                                return line.MethodName == methodName && line.MethodSignature == methodSignature;
                                            })
                                            .ToArray();

        if (filteredLines.Length is 0)
            throw new CoverageCalculationException("No lines found for the specified method");

        return filteredLines.CalculateCoverage(coverageType);
    }
}