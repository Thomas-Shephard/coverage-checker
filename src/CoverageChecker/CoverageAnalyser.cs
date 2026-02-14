using CoverageChecker.Parsers;
using CoverageChecker.Results;
using CoverageChecker.Services;
using CoverageChecker.Utils;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;

namespace CoverageChecker;

/// <summary>
/// Analyses coverage information
/// </summary>
public partial class CoverageAnalyser
{
    private readonly CoverageAnalyserOptions _options;
    private readonly IFileFinder _fileFinder;
    private readonly IParserFactory _parserFactory;
    private readonly IGitService _gitService;
    private readonly IDeltaCoverageService _deltaCoverageService;
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
        : this(options, fileFinder, new ParserFactory(new CoverageMergeService(loggerFactory?.CreateLogger<CoverageMergeService>())), new GitService(new ProcessExecutor(options.Directory)), new DeltaCoverageService(new CoverageMergeService(loggerFactory?.CreateLogger<CoverageMergeService>())), loggerFactory) { }

    internal CoverageAnalyser(CoverageAnalyserOptions options, IFileFinder fileFinder, IParserFactory parserFactory, IGitService gitService, IDeltaCoverageService deltaCoverageService, ILoggerFactory? loggerFactory = null)
    {
        _options = options;
        _fileFinder = fileFinder;
        _parserFactory = parserFactory;
        _gitService = gitService;
        _deltaCoverageService = deltaCoverageService;
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

        string root = rootDirectory ?? _options.Directory;

        Matcher matcher = new();
        if (_options.Include != null && _options.Include.Any())
        {
            matcher.AddGlobPatterns(_options.Include);
        }
        else
        {
            matcher.AddInclude("**/*");
        }

        if (_options.Exclude != null && _options.Exclude.Any())
        {
            matcher.AddGlobPatterns(_options.Exclude.Select(e => e.StartsWith('!') ? e : $"!{e}"));
        }

        List<FileCoverage> filesToRemove = [];
        foreach (FileCoverage file in coverage.Files)
        {
            string relativePath = PathUtils.NormalizePath(Path.GetRelativePath(root, file.Path));
            if (!matcher.Match(relativePath).HasMatches)
            {
                filesToRemove.Add(file);
            }
        }

        foreach (FileCoverage file in filesToRemove)
        {
            coverage.RemoveFile(file);
        }
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Finding coverage files in {Directory}...")]
    private partial void LogFindingCoverageFiles(string directory);

    [LoggerMessage(Level = LogLevel.Information, Message = "Found {Count} coverage files.")]
    private partial void LogFoundCoverageFiles(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Parsing coverage file: {FilePath}")]
    private partial void LogParsingCoverageFile(string filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to get git repository root. Assuming not in a git repository.")]
    private partial void LogGitRepoRootFailure(Exception ex);
}
