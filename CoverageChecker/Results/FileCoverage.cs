using CoverageChecker.Utils;

namespace CoverageChecker.Results;

public class FileCoverage(LineCoverage[] lines, string path, string? packageName = null) {
    public LineCoverage[] Lines { get; } = lines;
    public string Path { get; } = path;
    public string? PackageName { get; } = packageName;

    public bool TryGetLine(int lineNumber, out LineCoverage? line) {
        line = Lines.FirstOrDefault(line => line.LineNumber == lineNumber);
        return line is not null;
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