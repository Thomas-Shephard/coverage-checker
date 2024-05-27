namespace CoverageChecker.Results;

public class Coverage(FileCoverage[] files) {
    public FileCoverage[] Files { get; } = files;

    public bool TryGetFile(string path, out FileCoverage? file) {
        file = Files.FirstOrDefault(file => file.Path == path);
        return file is not null;
    }
}