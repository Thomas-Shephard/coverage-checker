using CoverageChecker.Parsers;
using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker;

public class CoverageAnalyser {
    private readonly CoverageFormat _coverageFormat;
    private readonly string _directory;
    private readonly string[] _globPatterns;

    public CoverageAnalyser(CoverageFormat coverageFormat, string directory, string globPattern) : this(coverageFormat, directory, [globPattern]) { }

    public CoverageAnalyser(CoverageFormat coverageFormat, string directory, IEnumerable<string> globPatterns) {
        globPatterns = globPatterns.ToArray();

        if (!globPatterns.Any()) {
            throw new ArgumentException("At least one glob pattern must be provided");
        }

        _coverageFormat = coverageFormat;
        _directory = directory;
        _globPatterns = globPatterns.ToArray();
    }

    public Coverage AnalyseCoverage() {
        string[] filePaths = GlobUtils.GetFilePathsFromGlobPatterns(_directory, _globPatterns).ToArray();

        if (filePaths.Length is 0)
            throw new NoCoverageFilesFoundException();

        Coverage coverage = new();
        BaseParser parser = ParserFactory.CreateParser(_coverageFormat, coverage);

        foreach (string filePath in filePaths) {
            parser.ParseCoverage(filePath);
        }

        return coverage;
    }
}