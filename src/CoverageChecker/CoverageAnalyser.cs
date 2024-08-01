using CoverageChecker.Parsers;
using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker;

public class CoverageAnalyser(CoverageFormat coverageFormat, string directory, IEnumerable<string> globPatterns, bool failIfNoFilesFound = true) {
    public CoverageAnalyser(CoverageFormat coverageFormat, string directory, string globPattern, bool failIfNoFilesFound = true) : this(coverageFormat, directory, [globPattern], failIfNoFilesFound) { }

    public Coverage AnalyseCoverage() {
        string[] filePaths = GlobUtils.GetFilePathsFromGlobPatterns(directory, globPatterns).ToArray();

        if (failIfNoFilesFound && filePaths.Length is 0)
            throw new CoverageParseException("No coverage files found");

        Coverage coverage = new();

        BaseParser parser = coverageFormat switch {
            CoverageFormat.Cobertura => new CoberturaParser(coverage),
            CoverageFormat.SonarQube => new SonarQubeParser(coverage),
            _                        => throw new ArgumentOutOfRangeException(nameof(coverageFormat), coverageFormat, "Unknown coverage format")
        };

        foreach (string filePath in filePaths) {
            parser.ParseCoverageFromFilePath(filePath);
        }

        return coverage;
    }
}