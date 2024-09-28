using CoverageChecker.Parsers;
using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker;

public class CoverageAnalyser(CoverageFormat coverageFormat, string directory, IEnumerable<string> globPatterns) {
    public CoverageAnalyser(CoverageFormat coverageFormat, string directory, string globPattern) : this(coverageFormat, directory, [globPattern]) { }

    public Coverage AnalyseCoverage() {
        string[] filePaths = GlobUtils.GetFilePathsFromGlobPatterns(directory, globPatterns).ToArray();

        if (filePaths.Length is 0)
            throw new NoCoverageFilesFoundException();

        Coverage coverage = new();

        BaseParser parser = coverageFormat switch {
            CoverageFormat.Cobertura => new CoberturaParser(coverage),
            CoverageFormat.SonarQube => new SonarQubeParser(coverage),
            _                        => throw new ArgumentOutOfRangeException(nameof(coverageFormat), coverageFormat, "Unknown coverage format")
        };

        foreach (string filePath in filePaths) {
            parser.ParseCoverage(filePath);
        }

        return coverage;
    }
}