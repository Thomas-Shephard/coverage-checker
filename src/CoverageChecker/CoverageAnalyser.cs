using CoverageChecker.Parsers;
using CoverageChecker.Results;
using CoverageChecker.Services;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoverageChecker;

/// <summary>
/// Analyses coverage information
/// </summary>
public partial class CoverageAnalyser
{
    /// <summary>
    /// The default epsilon value used for floating point comparisons.
    /// </summary>
    public const double DefaultEpsilon = 0.0001;

    private readonly CoverageFormat _coverageFormat;
    private readonly string _directory;
    private readonly IFileFinder _fileFinder;
    private readonly IParserFactory _parserFactory;
    private readonly IGitService _gitService;
    private readonly IDeltaCoverageService _deltaCoverageService;
    private readonly ICoverageRegressionService _coverageRegressionService;
    private readonly ILogger<CoverageAnalyser> _logger;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageAnalyser"/> class with a single glob pattern.
    /// </summary>
    /// <param name="coverageFormat">The format of the coverage file.</param>
    /// <param name="directory">The directory to search for coverage files within.</param>
    /// <param name="globPattern">The glob pattern to use to search for coverage files.</param>
    /// <param name="loggerFactory">The logger factory to use for logging.</param>
    public CoverageAnalyser(CoverageFormat coverageFormat, string directory, string globPattern, ILoggerFactory? loggerFactory = null) : this(coverageFormat, directory, [globPattern], loggerFactory) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageAnalyser"/> class with multiple glob patterns.
    /// </summary>
    /// <param name="coverageFormat">The format of the coverage file.</param>
    /// <param name="directory">The directory to search for coverage files within.</param>
    /// <param name="globPatterns">The glob patterns to use to search for coverage files.</param>
    /// <param name="loggerFactory">The logger factory to use for logging.</param>
    public CoverageAnalyser(CoverageFormat coverageFormat, string directory, IEnumerable<string> globPatterns, ILoggerFactory? loggerFactory = null) : this(coverageFormat, directory, new FileFinder(globPatterns, loggerFactory?.CreateLogger<FileFinder>()), loggerFactory) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageAnalyser"/> class with a <see cref="Matcher"/>.
    /// </summary>
    /// <param name="coverageFormat">The format of the coverage file.</param>
    /// <param name="directory">The directory to search for coverage files within.</param>
    /// <param name="matcher">The matcher to use to search for coverage files.</param>
    /// <param name="loggerFactory">The logger factory to use for logging.</param>
    public CoverageAnalyser(CoverageFormat coverageFormat, string directory, Matcher matcher, ILoggerFactory? loggerFactory = null) : this(coverageFormat, directory, new FileFinder(matcher, loggerFactory?.CreateLogger<FileFinder>()), loggerFactory) { }

    internal CoverageAnalyser(CoverageFormat coverageFormat, string directory, IFileFinder fileFinder, ILoggerFactory? loggerFactory = null)
        : this(coverageFormat, directory, fileFinder, loggerFactory, new CoverageMergeService(loggerFactory?.CreateLogger<CoverageMergeService>())) { }

    private CoverageAnalyser(CoverageFormat coverageFormat, string directory, IFileFinder fileFinder, ILoggerFactory? loggerFactory, ICoverageMergeService mergeService)
        : this(coverageFormat, directory, fileFinder, new ParserFactory(mergeService), new GitService(new ProcessExecutor(directory)), new DeltaCoverageService(mergeService), new CoverageRegressionService(), loggerFactory) { }

    internal CoverageAnalyser(CoverageFormat coverageFormat, string directory, IFileFinder fileFinder, IParserFactory parserFactory, IGitService gitService, IDeltaCoverageService deltaCoverageService, ICoverageRegressionService coverageRegressionService, ILoggerFactory? loggerFactory = null)
    {
        _coverageFormat = coverageFormat;
        _directory = directory;
        _fileFinder = fileFinder;
        _parserFactory = parserFactory;
        _gitService = gitService;
        _deltaCoverageService = deltaCoverageService;
        _coverageRegressionService = coverageRegressionService;
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _logger = _loggerFactory.CreateLogger<CoverageAnalyser>();
    }

    /// <summary>
    /// Analyses the coverage information from the given glob patterns in the specified directory.
    /// </summary>
    /// <returns>The coverage information.</returns>
    /// <exception cref="NoCoverageFilesFoundException">Thrown when no coverage files are found.</exception>
    public Coverage AnalyseCoverage()
    {
        LogFindingCoverageFiles(_directory);
        string[] filePaths = _fileFinder.FindFiles(_directory).ToArray();

        if (filePaths.Length is 0)
            throw new NoCoverageFilesFoundException();

        LogFoundCoverageFiles(filePaths.Length);

        string? rootDirectory = null;
        try
        {
            rootDirectory = _gitService.GetRepoRoot();
        }
        catch (GitException ex)
        {
            LogGitRepoRootFailure(ex);
            // Fallback to current directory if not in a git repo
        }

        Coverage coverage = new();
        ICoverageParser parser = _parserFactory.CreateParser(_coverageFormat, coverage, _loggerFactory);

        foreach (string filePath in filePaths)
        {
            LogParsingCoverageFile(filePath);
            parser.ParseCoverage(filePath, rootDirectory);
        }

        return coverage;
    }

    /// <summary>
    /// Analyses the coverage information and filters it to only include changed lines.
    /// </summary>
    /// <param name="baseBranch">The base branch to compare against for delta coverage.</param>
    /// <param name="coverage">Optional: The coverage information to filter. If not provided, it will be analysed from the files.</param>
    /// <returns>The delta coverage information and status.</returns>
    /// <exception cref="GitException">Thrown when there is an error retrieving changed lines from git.</exception>
    /// <exception cref="NoCoverageFilesFoundException">Thrown when no coverage files are found and coverage is not provided.</exception>
    public DeltaResult AnalyseDeltaCoverage(string baseBranch, Coverage? coverage = null)
    {
        coverage ??= AnalyseCoverage();
        IDictionary<string, HashSet<int>> changedLines = _gitService.GetChangedLines(baseBranch);
        return _deltaCoverageService.FilterCoverage(coverage, changedLines);
    }

    /// <summary>
    /// Checks for regression between the baseline and current coverage.
    /// </summary>
    /// <param name="baseline">The baseline coverage to compare against.</param>
    /// <param name="current">The current coverage.</param>
    /// <param name="epsilon">The epsilon value to use for comparison.</param>
    /// <returns>The regression result.</returns>
    public RegressionResult CheckRegression(Coverage baseline, Coverage current, double epsilon = DefaultEpsilon)
    {
        return _coverageRegressionService.CheckRegression(baseline, current, epsilon);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Finding coverage files in {Directory}...")]
    private partial void LogFindingCoverageFiles(string directory);

    [LoggerMessage(Level = LogLevel.Information, Message = "Found {Count} coverage files.")]
    private partial void LogFoundCoverageFiles(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Parsing coverage file: {FilePath}")]
    private partial void LogParsingCoverageFile(string filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to get git repository root. Assuming not in a git repository.")]
    private partial void LogGitRepoRootFailure(Exception ex);
}
