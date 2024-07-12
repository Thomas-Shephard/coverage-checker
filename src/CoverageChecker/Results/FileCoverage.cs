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

    internal void AddLine(int lineNumber, bool isCovered, int? branches = null, int? coveredBranches = null, string? className = null, string? methodName = null, string? methodSignature = null) {
        LineCoverage line = new(lineNumber, isCovered, branches, coveredBranches, className, methodName, methodSignature);

        LineCoverage? existingLine = GetLine(lineNumber);

        if (existingLine is not null) {
            // If the lines are the same (excluding method name and signature), there is no need to update it
            if (existingLine.EquivalentTo(line)) {
                return;
            }

            // If the lines are not substantively the same, throw an exception
            if (existingLine.Branches != line.Branches || existingLine.ClassName != line.ClassName) {
                throw new CoverageCalculationException("Line already exists in the file");
            }

            // Update the existing line with the new coverage information
            existingLine.IsCovered = existingLine.IsCovered || line.IsCovered;
            if (existingLine.Branches is not null) {
                existingLine.CoveredBranches = Math.Max(existingLine.CoveredBranches ?? 0, line.CoveredBranches ?? 0);
            }

            return;
        }

        _lines.Add(line);
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