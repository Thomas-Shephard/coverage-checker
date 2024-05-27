namespace CoverageChecker.Results;

public class FileCoverage(LineCoverage[] lines) {
    public LineCoverage[] Lines { get; } = lines;
}