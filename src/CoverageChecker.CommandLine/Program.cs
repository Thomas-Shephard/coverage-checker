using CommandLine;
using CommandLine.Text;
using CoverageChecker;
using CoverageChecker.CommandLine;
using CoverageChecker.Results;
using CoverageChecker.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

Parser parser = new(with => with.HelpWriter = null);
ParserResult<object> parserResult = parser.ParseArguments<CheckOptions, RunOptions>(args);

return await parserResult.MapResult(
    (CheckOptions options) => Run(options),
    (RunOptions options) => RunCommandHandler.Run(options, Run, CreateLoggerFactory),
    _ => Task.FromResult(DisplayHelp(parserResult)));

static async Task<int> Run(CommandLineOptions options, bool? isGitHubActionsParam = null, ILoggerFactory? loggerFactory = null)
{
    bool isGitHubActions = isGitHubActionsParam ?? Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";

    bool shouldDispose = loggerFactory == null;
    loggerFactory ??= CreateLoggerFactory(isGitHubActions);

    try
    {
        ILogger logger = loggerFactory.CreateLogger("CoverageChecker.CommandLine");

        CoverageAnalyser coverageAnalyser = CreateCoverageAnalyser(options, loggerFactory);

        if (!TryAnalyseCoverage(coverageAnalyser, logger, out Coverage? coverage))
        {
            return 1;
        }

        CoverageResult result = CreateInitialResult(coverage);

        logger.LogLineCoverage(result.LineCoverage);
        logger.LogBranchCoverage(result.BranchCoverage);

        if (options.Delta && !TryHandleDeltaCoverage(coverageAnalyser, coverage, options, logger, ref result))
        {
            return 1;
        }

        if (isGitHubActions)
        {
            await GitHubReportWriter.WriteSummary(result, options, logger);
        }

        return CheckThresholds(result, options, isGitHubActions, logger);
    }
    finally
    {
        if (shouldDispose)
        {
            loggerFactory.Dispose();
        }
    }
}

static CoverageAnalyser CreateCoverageAnalyser(CommandLineOptions options, ILoggerFactory loggerFactory)
{
    CoverageAnalyserOptions analyserOptions = new()
    {
        CoverageFormat = options.CoverageFormat,
        Directory = options.Directory,
        GlobPatterns = options.GlobPatterns,
        Include = options.Include,
        Exclude = options.Exclude,
        RenameThreshold = options.RenameThreshold
    };

    return new CoverageAnalyser(analyserOptions, loggerFactory);
}

static CoverageResult CreateInitialResult(Coverage coverage)
{
    return new CoverageResult(
        coverage,
        coverage.CalculateOverallCoverage(),
        coverage.CalculateOverallCoverage(CoverageType.Branch),
        null,
        double.NaN,
        double.NaN,
        false,
        false,
        false,
        []
    );
}

static bool TryHandleDeltaCoverage(CoverageAnalyser coverageAnalyser, Coverage coverage, CommandLineOptions options, ILogger logger, ref CoverageResult result)
{
    if (!TryAnalyseDeltaCoverage(coverageAnalyser, coverage, options, logger, out DeltaResult? deltaResult))
    {
        return false;
    }

    if (deltaResult.HasChangedLines)
    {
        Coverage deltaCoverage = deltaResult.Coverage;
        result = result with
        {
            DeltaCoverage = deltaCoverage,
            HasDeltaChangedLines = true,
            HasGitDeltaChangedLines = deltaResult.HasGitChangedLines,
            HasChangedCoverageFiles = deltaResult.HasChangedCoverageFiles,
            ChangedFilesMissingCoverage = deltaResult.ChangedFilesMissingCoverage,
            DeltaLineCoverage = deltaCoverage.CalculateOverallCoverage(),
            DeltaBranchCoverage = deltaCoverage.CalculateOverallCoverage(CoverageType.Branch)
        };

        logger.LogDeltaLineCoverage(result.DeltaLineCoverage);
        logger.LogDeltaBranchCoverage(result.DeltaBranchCoverage);
    }
    else if (deltaResult.HasChangedCoverageFiles)
    {
        result = result with
        {
            HasGitDeltaChangedLines = deltaResult.HasGitChangedLines,
            HasChangedCoverageFiles = true,
            ChangedFilesMissingCoverage = deltaResult.ChangedFilesMissingCoverage
        };
        logger.LogDeltaLinesMissingFromCoverage();
    }
    else if (deltaResult.HasGitChangedLines)
    {
        result = result with
        {
            HasGitDeltaChangedLines = true,
            ChangedFilesMissingCoverage = deltaResult.ChangedFilesMissingCoverage
        };
        logger.LogNoDeltaLinesFound();
    }
    else
    {
        logger.LogNoDeltaLinesFound();
    }

    return true;
}

static bool TryAnalyseDeltaCoverage(CoverageAnalyser analyser, Coverage coverage, CommandLineOptions options, ILogger logger, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out DeltaResult? deltaResult)
{
    deltaResult = null;
    try
    {
        deltaResult = analyser.AnalyseDeltaCoverage(options.DeltaBase, coverage, options.StrictDelta);
        return true;
    }
    catch (Exception ex) when (ex is GitException or ArgumentException)
    {
        logger.LogDeltaAnalysisFailed(ex);
        return false;
    }
}

static int CheckThresholds(CoverageResult result, CommandLineOptions options, bool isGitHubActions, ILogger logger)
{
    bool failed = EvaluateOverallThresholds(result, options, logger);

    if (options.Delta && result is { HasDeltaChangedLines: true, DeltaCoverage: not null })
    {
        failed |= EvaluateDeltaThresholds(result, options, logger);
    }
    else if (options.Delta && result.HasChangedCoverageFiles)
    {
        failed = true;
    }

    if (ShouldFailStrictDelta(result, options))
    {
        logger.LogStrictDeltaFilesMissingFromCoverage(
            result.ChangedFilesMissingCoverage.Count,
            FormatStrictDeltaMissingFiles(result.ChangedFilesMissingCoverage));
        failed = true;
    }

    if (!failed)
    {
        logger.LogThresholdMet();
        return 0;
    }

    if (!isGitHubActions)
    {
        Coverage coverageToReport = options.Delta && result is { HasDeltaChangedLines: true, DeltaCoverage: not null }
            ? result.DeltaCoverage
            : result.OverallCoverage;
        ReportGaps(coverageToReport, logger);
    }

    return 1;
}

static bool ShouldFailStrictDelta(CoverageResult result, CommandLineOptions options)
{
    return options is { Delta: true, StrictDelta: true } && result.ChangedFilesMissingCoverage.Count > 0;
}

static string FormatStrictDeltaMissingFiles(IReadOnlyList<string> files)
{
    string baseDirectory = PathUtils.GetNormalizedFullPath(Environment.CurrentDirectory);

    IEnumerable<string> displayPaths = files
        .Take(5)
        .Select(file => FormatDisplayPath(file, baseDirectory));

    string suffix = files.Count > 5 ? ", ..." : string.Empty;
    return string.Join(", ", displayPaths) + suffix;
}

static string FormatDisplayPath(string file, string baseDirectory)
{
    string normalizedFile = PathUtils.NormalizePath(file);

    if (!Path.IsPathRooted(normalizedFile))
    {
        return normalizedFile;
    }

    string relativePath = PathUtils.NormalizePath(Path.GetRelativePath(baseDirectory, normalizedFile));
    return relativePath.StartsWith("..", StringComparison.Ordinal) ? normalizedFile : relativePath;
}

static bool EvaluateOverallThresholds(CoverageResult result, CommandLineOptions options, ILogger logger)
{
    bool failed = false;
    if (double.IsNaN(result.LineCoverage))
    {
        logger.LogNoApplicableLineCoverage();
        failed = true;
    }
    else if (options.LineThreshold > result.LineCoverage)
    {
        logger.LogLineCoverageBelowThreshold(result.LineCoverage, options.LineThreshold);
        failed = true;
    }

    if (options.BranchThreshold > result.BranchCoverage)
    {
        logger.LogBranchCoverageBelowThreshold(result.BranchCoverage, options.BranchThreshold);
        failed = true;
    }

    return failed;
}

static bool EvaluateDeltaThresholds(CoverageResult result, CommandLineOptions options, ILogger logger)
{
    bool failed = false;
    double deltaLineThreshold = options.EffectiveDeltaLineThreshold;
    double deltaBranchThreshold = options.EffectiveDeltaBranchThreshold;

    if (double.IsNaN(result.DeltaLineCoverage))
    {
        logger.LogNoApplicableDeltaLineCoverage();
        failed = true;
    }
    else if (deltaLineThreshold > result.DeltaLineCoverage)
    {
        logger.LogDeltaLineCoverageBelowThreshold(result.DeltaLineCoverage, deltaLineThreshold);
        failed = true;
    }

    if (!double.IsNaN(result.DeltaBranchCoverage) && deltaBranchThreshold > result.DeltaBranchCoverage)
    {
        logger.LogDeltaBranchCoverageBelowThreshold(result.DeltaBranchCoverage, deltaBranchThreshold);
        failed = true;
    }

    return failed;
}

static void ReportGaps(Coverage coverage, ILogger logger)
{
    var problematicFiles = coverage.Files
                                   .Select(f => new
                                   {
                                       File = f,
                                       LineCov = f.CalculateFileCoverage(),
                                       BranchCov = f.CalculateFileCoverage(CoverageType.Branch)
                                   })
                                   .Where(f => f.LineCov < 1.0 || (f.BranchCov < 1.0 && !double.IsNaN(f.BranchCov)))
                                   .OrderBy(f => double.IsNaN(f.BranchCov) ? f.LineCov : Math.Min(f.LineCov, f.BranchCov))
                                   .Take(5); // Limit to top 5 to avoid spam

    foreach (FileCoverage file in problematicFiles.Select(item => item.File))
    {
        logger.LogFileGapHeader(file.Path);

        string lineGaps = GapReportUtils.GetLineGaps(file.Lines);
        if (!string.IsNullOrEmpty(lineGaps))
        {
            logger.LogUncoveredLines(lineGaps);
        }

        List<(int LineNumber, int Covered, int Total)> branchGaps = GapReportUtils.GetBranchGaps(file.Lines).ToList();
        if (branchGaps.Count > 0)
        {
            string branches = string.Join(", ", branchGaps.Select(b => $"Line {b.LineNumber} ({b.Covered}/{b.Total})"));
            logger.LogPartialBranches(branches);
        }
    }
}

static ILoggerFactory CreateLoggerFactory(bool isGitHubActions)
{
    return LoggerFactory.Create(builder =>
    {
        if (isGitHubActions)
        {
            builder.AddConsole(opt => opt.FormatterName = "github")
                   .AddConsoleFormatter<GitHubWorkflowFormatter, ConsoleFormatterOptions>();
            builder.AddFilter("CoverageChecker", LogLevel.Information);
        }
        else
        {
            builder.AddConsole(opt => opt.FormatterName = "clean")
                   .AddConsoleFormatter<ConsoleLogFormatter, ConsoleFormatterOptions>();
            builder.AddFilter("CoverageChecker", LogLevel.Warning);
        }

        builder.SetMinimumLevel(LogLevel.Information);
        builder.AddFilter("CoverageChecker.CommandLine", LogLevel.Information);
    });
}

static bool TryAnalyseCoverage(CoverageAnalyser coverageAnalyser, ILogger logger, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Coverage? coverage)
{
    try
    {
        coverage = coverageAnalyser.AnalyseCoverage();
        logger.LogParsedCoverage(coverage.Files.Count);
        return true;
    }
    catch (NoCoverageFilesFoundException)
    {
        logger.LogNoCoverageFilesFound();
    }
    catch (CoverageParseException exception)
    {
        logger.LogErrorParsingCoverageFiles(exception);
    }

    coverage = null;
    return false;
}

static int DisplayHelp<T>(ParserResult<T> result)
{
    HelpText? helpText = HelpText.AutoBuild(result, helpText =>
    {
        helpText.AddEnumValuesToHelpText = true;
        return helpText;
    }, e => e);

    Console.WriteLine(helpText);
    return 1;
}
