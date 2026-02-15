using CoverageChecker.Parsers;
using CoverageChecker.Results;
using CoverageChecker.Services;
using CoverageChecker.Utils;
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
    /// The default epsilon value used for floating point comparisons (0.0001 = 0.01%).
    /// </summary>
    public const double DefaultEpsilon = 0.0001;

    private readonly CoverageAnalyserOptions _options;
    private readonly IFileFinder _fileFinder;
    private readonly IParserFactory _parserFactory;
    private readonly IGitService _gitService;
    private readonly IDeltaCoverageService _deltaCoverageService;
    private readonly ICoverageRegressionService _coverageRegressionService;
    private readonly ILogger<CoverageAnalyser> _logger;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageAnalyser"/> class with the specified options.
    /// </summary>
    /// <param name="options">The options to use for analysis.</param>
    /// <param name="loggerFactory">The logger factory to use for logging.</param>
    public CoverageAnalyser(CoverageAnalyserOptions options, ILoggerFactory? loggerFactory = null)
        : this(options, new FileFinder(options.GlobPatterns, loggerFactory?.CreateLogger<FileFinder>()), loggerFactory) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageAnalyser"/> class with a <see cref="Matcher"/>.
    /// </summary>
    /// <param name="options">The options to use for analysis (GlobPatterns will be ignored in favor of the matcher).</param>
    /// <param name="matcher">The matcher to use to search for coverage files.</param>
    /// <param name="loggerFactory">The logger factory to use for logging.</param>
    public CoverageAnalyser(CoverageAnalyserOptions options, Matcher matcher, ILoggerFactory? loggerFactory = null)
        : this(options, new FileFinder(matcher, loggerFactory?.CreateLogger<FileFinder>()), loggerFactory) { }

    internal CoverageAnalyser(CoverageAnalyserOptions options, IFileFinder fileFinder, ILoggerFactory? loggerFactory = null)
        : this(options, fileFinder, new ParserFactory(new CoverageMergeService(loggerFactory?.CreateLogger<CoverageMergeService>())), new GitService(new ProcessExecutor(options.Directory)), new DeltaCoverageService(new CoverageMergeService(loggerFactory?.CreateLogger<CoverageMergeService>())), new CoverageRegressionService(), loggerFactory) { }

    internal CoverageAnalyser(CoverageAnalyserOptions options, IFileFinder fileFinder, IParserFactory parserFactory, IGitService gitService, IDeltaCoverageService deltaCoverageService, ICoverageRegressionService coverageRegressionService, ILoggerFactory? loggerFactory = null)
    {
        _options = options;
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
        LogFindingCoverageFiles(_options.Directory);
        string[] filePaths = _fileFinder.FindFiles(_options.Directory).ToArray();

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
        Dictionary<CoverageFormat, ICoverageParser> parsers = [];
        foreach (string filePath in filePaths)
        {
            LogParsingCoverageFile(filePath);
            CoverageFormat format = _options.CoverageFormat == CoverageFormat.Auto
                ? _parserFactory.DetectFormat(filePath)
                : _options.CoverageFormat;

            if (!parsers.TryGetValue(format, out ICoverageParser? parser))
            {
                parser = _parserFactory.CreateParser(format, coverage, _loggerFactory);
                parsers[format] = parser;
            }

            parser.ParseCoverage(filePath, rootDirectory);
        }

        FilterFiles(coverage, rootDirectory);

        return coverage;
    }

    private void FilterFiles(Coverage coverage, string? rootDirectory)
    {
        if (_options.Include == null && _options.Exclude == null) return;

        string root = rootDirectory ?? Environment.CurrentDirectory;
        Matcher matcher = CreateMatcher();

        foreach (FileCoverage file in coverage.Files.Where(f => IsFileExcluded(f, root, matcher)).ToList())
        {
            coverage.RemoveFile(file);
        }
    }

    private Matcher CreateMatcher()
    {
        Matcher matcher = new();
        string[] include = _options.Include?.ToArray() ?? [];
        string[] exclude = _options.Exclude?.ToArray() ?? [];

        if (include.Any(p => !p.StartsWith('!')))
        {
            matcher.AddGlobPatterns(include);
        }
        else
        {
            matcher.AddInclude("**/*");
            if (include.Length > 0)
            {
                matcher.AddGlobPatterns(include);
            }
        }

        if (exclude.Length > 0)
        {
            foreach (string pattern in exclude)
            {
                matcher.AddExclude(pattern.StartsWith('!') ? pattern[1..] : pattern);
            }
        }

        return matcher;
    }

    private static bool IsFileExcluded(FileCoverage file, string root, Matcher matcher)
    {
        if (Path.GetPathRoot(root) != Path.GetPathRoot(file.Path))
        {
            return true;
        }

        string relativePath = PathUtils.NormalizePath(Path.GetRelativePath(root, file.Path));
        return !matcher.Match(relativePath).HasMatches;
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
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);
        return _coverageRegressionService.CheckRegression(baseline, current, null, epsilon);
    }

    /// <summary>
    /// Checks for regression between the baseline and current coverage, using git to detect renames.
    /// </summary>
    /// <param name="baseline">The baseline coverage to compare against.</param>
    /// <param name="current">The current coverage.</param>
    /// <param name="baseRef">The base git reference (branch or commit) that represents the baseline state.</param>
    /// <param name="headRef">The head git reference (branch or commit) that represents the current state. Defaults to "HEAD".</param>
    /// <param name="epsilon">The epsilon value to use for comparison.</param>
    /// <returns>The regression result.</returns>
    public RegressionResult CheckRegression(Coverage baseline, Coverage current, string baseRef, string headRef = "HEAD", double epsilon = DefaultEpsilon)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);
        IDictionary<string, string> renames = _gitService.GetRenames(baseRef, headRef, _options.RenameThreshold);
        return _coverageRegressionService.CheckRegression(baseline, current, renames, epsilon);
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
