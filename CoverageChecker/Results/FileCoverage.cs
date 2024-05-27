namespace CoverageChecker.Results;

public class FileCoverage(LineCoverage[] lines, string path, string? packageName = null) {
    public LineCoverage[] Lines { get; } = lines;
    public string Path { get; } = path;
    public string? PackageName { get; } = packageName;
}