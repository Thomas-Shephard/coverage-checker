namespace CoverageChecker.Results;

public class FileCoverage(LineCoverage[] lines, string path, string? packageName = null) {
    public LineCoverage[] Lines { get; } = lines;
    public string Path { get; } = path;
    public string? PackageName { get; } = packageName;

    public bool TryGetLine(int lineNumber, out LineCoverage? line) {
        line = Lines.FirstOrDefault(line => line.LineNumber == lineNumber);
        return line is not null;
    }
}