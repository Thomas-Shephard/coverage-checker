using CoverageChecker.Parsers;
using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker;

/// <summary>
/// Analyses coverage information
/// </summary>
public class CoverageAnalyser {
    private readonly CoverageFormat _coverageFormat;
    private readonly string _directory;
    private readonly string[] _globPatterns;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageAnalyser"/> class with a single glob pattern.
    /// </summary>
    /// <param name="coverageFormat">The format of the coverage file.</param>
    /// <param name="directory">The directory to search for coverage files within.</param>
    /// <param name="globPattern">The glob pattern to use to search for coverage files.</param>
    public CoverageAnalyser(CoverageFormat coverageFormat, string directory, string globPattern) : this(coverageFormat, directory, [globPattern]) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageAnalyser"/> class with multiple glob patterns.
    /// </summary>
    /// <param name="coverageFormat">The format of the coverage file.</param>
    /// <param name="directory">The directory to search for coverage files within.</param>
    /// <param name="globPatterns">The glob patterns to use to search for coverage files.</param>
    /// <exception cref="ArgumentException">Thrown when no glob patterns are provided.</exception>
    public CoverageAnalyser(CoverageFormat coverageFormat, string directory, IEnumerable<string> globPatterns) {
        globPatterns = globPatterns.ToArray();

        if (!globPatterns.Any()) {
            throw new ArgumentException("At least one glob pattern must be provided");
        }

        _coverageFormat = coverageFormat;
        _directory = directory;
        _globPatterns = globPatterns.ToArray();
    }

    /// <summary>
    /// Analyses the coverage information from the given glob patterns in the specified directory.
    /// </summary>
    /// <returns>The coverage information.</returns>
    /// <exception cref="NoCoverageFilesFoundException">Thrown when no coverage files are found.</exception>
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