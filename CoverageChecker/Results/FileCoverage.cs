using CoverageChecker.Utils;

namespace CoverageChecker.Results;

public class FileCoverage(string path, string? packageName = null) {
    internal FileCoverage(IEnumerable<LineCoverage> lines, string path, string? packageName = null) : this(path, packageName) {
        _lines = lines.ToList();
    }

    public string Path { get; } = path;
    public string? PackageName { get; } = packageName;
    public IReadOnlyList<LineCoverage> Lines => _lines.AsReadOnly();
    private readonly List<LineCoverage> _lines = [];

    public LineCoverage? GetLine(int lineNumber) {
        return Lines.FirstOrDefault(line => line.LineNumber == lineNumber);
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