namespace CoverageChecker.Results;

public class Coverage(FileCoverage[] files) {
    public FileCoverage[] Files { get; } = files;
}