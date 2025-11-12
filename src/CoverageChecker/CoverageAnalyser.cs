using CoverageChecker.Parsers;
using CoverageChecker.Results;
using CoverageChecker.Services;
using CoverageChecker.Utils;
using Microsoft.Extensions.FileSystemGlobbing;

namespace CoverageChecker;

/// <summary>
/// Analyses coverage information
/// </summary>
public class CoverageAnalyser
{
    private readonly CoverageFormat _coverageFormat;
    private readonly string _directory;
    private readonly Matcher _matcher;
    private readonly ICoverageCalculationService _coverageCalculationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageAnalyser"/> class with a single glob pattern.
    /// </summary>
    /// <param name="coverageFormat">The format of the coverage file.</param>
    /// <param name="directory">The directory to search for coverage files within.</param>
    /// <param name="globPattern">The glob pattern to use to search for coverage files.</param>
    /// <param name="coverageCalculationService">The coverage calculation service to use.</param>
    public CoverageAnalyser(CoverageFormat coverageFormat, string directory, string globPattern, ICoverageCalculationService? coverageCalculationService = null)
        : this(coverageFormat, directory, [globPattern], coverageCalculationService) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageAnalyser"/> class with multiple glob patterns.
    /// </summary>
    /// <param name="coverageFormat">The format of the coverage file.</param>
    /// <param name="directory">The directory to search for coverage files within.</param>
    /// <param name="globPatterns">The glob patterns to use to search for coverage files.</param>
    /// <param name="coverageCalculationService">The coverage calculation service to use.</param>
    public CoverageAnalyser(CoverageFormat coverageFormat, string directory, IEnumerable<string> globPatterns, ICoverageCalculationService? coverageCalculationService = null)
        : this(coverageFormat, directory, new Matcher().AddGlobPatterns(globPatterns), coverageCalculationService) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageAnalyser"/> class with a <see cref="Matcher"/>.
    /// </summary>
    /// <param name="coverageFormat">The format of the coverage file.</param>
    /// <param name="directory">The directory to search for coverage files within.</param>
    /// <param name="matcher">The matcher to use to search for coverage files.</param>
    /// <param name="coverageCalculationService">The coverage calculation service to use.</param>
    public CoverageAnalyser(CoverageFormat coverageFormat, string directory, Matcher matcher, ICoverageCalculationService? coverageCalculationService = null)
    {
        _coverageFormat = coverageFormat;
        _directory = directory;
        _matcher = matcher;
        _coverageCalculationService = coverageCalculationService ?? new CoverageCalculationService();
    }

    /// <summary>
    /// Analyses the coverage information from the given glob patterns in the specified directory.
    /// </summary>
    /// <returns>The coverage information.</returns>
    /// <exception cref="NoCoverageFilesFoundException">Thrown when no coverage files are found.</exception>
    public Coverage AnalyseCoverage()
    {
        string[] filePaths = _matcher.GetResultsInFullPath(_directory).ToArray();

        if (filePaths.Length is 0)
            throw new NoCoverageFilesFoundException();

        Coverage coverage = new(_coverageCalculationService);
        ParserBase parser = ParserFactory.CreateParser(_coverageFormat, coverage);

        foreach (string filePath in filePaths)
        {
            parser.ParseCoverage(filePath);
        }

        return coverage;
    }
}