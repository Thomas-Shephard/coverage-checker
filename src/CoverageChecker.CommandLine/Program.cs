using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
    (RunOptions options) => RunCommandAndCheck(options),
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
            await WriteGitHubSummary(result, options, logger);
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

static async Task<int> RunCommandAndCheck(RunOptions options)
{
    bool isGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
    using ILoggerFactory loggerFactory = CreateLoggerFactory(isGitHubActions);
    ILogger logger = loggerFactory.CreateLogger("CoverageChecker.CommandLine");
    string? tempDir = null;

    try
    {
        string workingDirectory = Path.GetFullPath(options.WorkingDirectory ?? Environment.CurrentDirectory);
        string outputDir = Path.GetFullPath(options.Output ?? Path.Combine(Path.GetTempPath(), "coverage-checker", Guid.NewGuid().ToString()));
        tempDir = options.Output == null ? outputDir : null;

        if (!Directory.Exists(workingDirectory))
        {
            logger.LogWorkingDirectoryNotFound(workingDirectory);
            return 1;
        }

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        string command = PrepareCommand(options.Command, outputDir, logger);

        int exitCode = await ExecuteCommand(command, workingDirectory, options.Timeout, logger);

        if (exitCode != 0)
        {
            if (options.ContinueOnFailure)
            {
                logger.LogCommandFailedWarning(exitCode);
            }
            else
            {
                logger.LogCommandFailed(exitCode);
                return exitCode;
            }
        }

        CommandLineOptions effectiveOptions = options with { Directory = outputDir };
        return await Run(effectiveOptions, isGitHubActions, loggerFactory);
    }
    catch (Exception ex)
    {
        logger.LogCriticalError(ex);
        return 1;
    }
    finally
    {
        CleanupTempDirectory(tempDir, logger);
    }
}

static string PrepareCommand(string commandTemplate, string outputDir, ILogger logger)
{
    string command = OutputPathSuffixRegex().Replace(
        commandTemplate,
        match => QuoteShellPath(outputDir + match.Groups[1].Value));

    command = command.Replace("{output}", QuoteShellPath(outputDir));
    logger.LogRunningCommand(command);
    return command;
}

static string QuoteShellPath(string path)
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        string normalizedPath = path.StartsWith(@"\\", StringComparison.Ordinal)
            ? @"\\" + path[2..].Replace('\\', '/')
            : path.Replace('\\', '/');

        return $"\"{normalizedPath.Replace("\"", "\"\"")}\"";
    }

    return $"'{path.Replace("'", "'\\''")}'";
}

static async Task<int> ExecuteCommand(string command, string workingDirectory, int timeoutMinutes, ILogger logger)
{
    using Process process = new();
    process.StartInfo.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "sh";
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        process.StartInfo.Arguments = $"/d /s /c \"{command}\"";
    }
    else
    {
        process.StartInfo.ArgumentList.Add("-c");
        process.StartInfo.ArgumentList.Add(command);
    }

    process.StartInfo.UseShellExecute = false;
    process.StartInfo.CreateNoWindow = true;
    process.StartInfo.WorkingDirectory = workingDirectory;
    // Do not redirect to allow inheriting the parent console's stdout/stderr (real-time output)
    process.StartInfo.RedirectStandardOutput = false;
    process.StartInfo.RedirectStandardError = false;

    process.Start();

    TimeSpan timeout = timeoutMinutes == -1
        ? Timeout.InfiniteTimeSpan
        : TimeSpan.FromMinutes(timeoutMinutes);
    using CancellationTokenSource cts = new(timeout);
    try
    {
        await process.WaitForExitAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        process.Kill(true);
        logger.LogCommandTimedOut(timeoutMinutes);
        return 1;
    }

    return process.ExitCode;
}

static void CleanupTempDirectory(string? tempDir, ILogger logger)
{
    if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
    {
        try
        {
            Directory.Delete(tempDir, true);
        }
        catch (Exception ex)
        {
            logger.LogCleanupFailed(ex, tempDir);
        }
    }
}

static bool TryAnalyseDeltaCoverage(CoverageAnalyser analyser, Coverage coverage, CommandLineOptions options, ILogger logger, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out DeltaResult? deltaResult)
{
    deltaResult = null;
    try
    {
        deltaResult = analyser.AnalyseDeltaCoverage(options.DeltaBase, coverage);
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

    if (options is { Delta: true, StrictDelta: true } && result.ChangedFilesMissingCoverage.Count > 0)
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

    if (double.IsNaN(result.DeltaLineCoverage))
    {
        logger.LogNoApplicableDeltaLineCoverage();
        failed = true;
    }
    else if (options.LineThreshold > result.DeltaLineCoverage)
    {
        logger.LogDeltaLineCoverageBelowThreshold(result.DeltaLineCoverage, options.LineThreshold);
        failed = true;
    }

    if (!double.IsNaN(result.DeltaBranchCoverage) && options.BranchThreshold > result.DeltaBranchCoverage)
    {
        logger.LogDeltaBranchCoverageBelowThreshold(result.DeltaBranchCoverage, options.BranchThreshold);
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

static async Task WriteGitHubSummary(CoverageResult result, CommandLineOptions options, ILogger logger)
{
    string? summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
    if (string.IsNullOrEmpty(summaryPath)) return;

    try
    {
        string summary = BuildGitHubSummary(result, options);
        await File.AppendAllTextAsync(summaryPath, summary);

        if (IsAnyThresholdViolated(result, options))
        {
            Coverage coverageToAnnotate = (options.Delta && result is { HasDeltaChangedLines: true, DeltaCoverage: not null })
                ? result.DeltaCoverage
                : result.OverallCoverage;

            string rootDirectory = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? options.Directory;
            rootDirectory = PathUtils.GetNormalizedFullPath(rootDirectory);

            EmitGitHubAnnotations(coverageToAnnotate, rootDirectory);
        }
    }
    catch (Exception ex)
    {
        logger.LogGitHubSummaryWriteFailed(ex, summaryPath);
    }
}

static string BuildGitHubSummary(CoverageResult result, CommandLineOptions options)
{
    StringBuilder summary = new();
    summary.AppendLine("### Coverage Report Summary");
    summary.AppendLine();
    summary.AppendLine("| Metric | Current | Threshold | Status |");
    summary.AppendLine("| :--- | :---: | :---: | :---: |");
    summary.AppendLine(FormatMetricRow("Line Coverage", result.LineCoverage, options.LineThreshold, failOnNaN: true));
    summary.AppendLine(FormatMetricRow("Branch Coverage", result.BranchCoverage, options.BranchThreshold));

    if (options.Delta)
    {
        AppendDeltaSummary(summary, result, options);
    }

    if (ShouldShowBreakdown(result.LineCoverage, result.BranchCoverage))
    {
        summary.Append(GetFileBreakdown(result.OverallCoverage));
    }

    if (result is { DeltaCoverage: not null, HasDeltaChangedLines: true } && ShouldShowBreakdown(result.DeltaLineCoverage, result.DeltaBranchCoverage))
    {
        summary.AppendLine();
        summary.AppendLine("#### Delta File Breakdown");
        summary.Append(GetFileBreakdown(result.DeltaCoverage));
    }

    return summary.ToString();
}

static void AppendDeltaSummary(StringBuilder summary, CoverageResult result, CommandLineOptions options)
{
    if (result.HasDeltaChangedLines)
    {
        summary.AppendLine(FormatMetricRow("Delta Line Coverage", result.DeltaLineCoverage, options.LineThreshold, failOnNaN: true));
        summary.AppendLine(FormatMetricRow("Delta Branch Coverage", result.DeltaBranchCoverage, options.BranchThreshold));
    }
    else
    {
        string message = result.HasChangedCoverageFiles
            ? "N/A (Changed lines missing from coverage data)"
            : "N/A (No changed lines)";
        string status = result.HasChangedCoverageFiles ? "❌" : "✅";
        summary.AppendLine(CultureInfo.InvariantCulture, $"| Delta Coverage | {message} | - | {status} |");
    }
}

static bool IsAnyThresholdViolated(CoverageResult result, CommandLineOptions options)
{
    if (double.IsNaN(result.LineCoverage) || options.LineThreshold > result.LineCoverage || options.BranchThreshold > result.BranchCoverage)
    {
        return true;
    }

    if (options.Delta && result.HasChangedCoverageFiles && !result.HasDeltaChangedLines)
    {
        return true;
    }

    if (options is { Delta: true, StrictDelta: true } && result.ChangedFilesMissingCoverage.Count > 0)
    {
        return true;
    }

    return options.Delta && result.HasDeltaChangedLines &&
           (double.IsNaN(result.DeltaLineCoverage) || options.LineThreshold > result.DeltaLineCoverage || options.BranchThreshold > result.DeltaBranchCoverage);
}

static void EmitGitHubAnnotations(Coverage coverage, string rootDirectory)
{
    int totalAnnotations = 0;
    const int maxTotalAnnotations = 50;

    foreach (FileCoverage file in coverage.Files.OrderBy(f => f.CalculateFileCoverage()))
    {
        if (totalAnnotations >= maxTotalAnnotations) break;

        EmitFileAnnotations(file, rootDirectory, ref totalAnnotations, maxTotalAnnotations);
    }
}

static void EmitFileAnnotations(FileCoverage file, string rootDirectory, ref int totalAnnotations, int maxTotalAnnotations)
{
    if (Path.GetPathRoot(rootDirectory) != Path.GetPathRoot(file.Path))
    {
        return;
    }

    string relativePath = PathUtils.NormalizePath(Path.GetRelativePath(rootDirectory, file.Path));
    string escapedPath = GitHubWorkflowFormatter.EscapeProperty(relativePath);

    EmitLineGaps(file, relativePath, escapedPath, ref totalAnnotations, maxTotalAnnotations);
    EmitBranchGaps(file, relativePath, escapedPath, ref totalAnnotations, maxTotalAnnotations);
}

static void EmitLineGaps(FileCoverage file, string relativePath, string escapedPath, ref int totalAnnotations, int maxTotalAnnotations)
{
    foreach ((int Start, int End) range in GapReportUtils.GetLineGapRanges(file.Lines).Take(10))
    {
        if (totalAnnotations >= maxTotalAnnotations) return;

        bool isSingleLine = range.Start == range.End;
        string lineInfo = isSingleLine ? range.Start.ToString(CultureInfo.InvariantCulture) : $"{range.Start}-{range.End}";
        string noun = isSingleLine ? "line" : "range";

        string title = GitHubWorkflowFormatter.EscapeProperty("Missing Line Coverage");
        string message = GitHubWorkflowFormatter.EscapeMessage($"[{relativePath} : {lineInfo}] This {noun} is not covered by tests.");
        string lineParams = isSingleLine ? $"line={range.Start}" : $"line={range.Start},endLine={range.End}";
        Console.WriteLine($"::warning file={escapedPath},{lineParams},title={title}::{message}");
        totalAnnotations++;
    }
}

static void EmitBranchGaps(FileCoverage file, string relativePath, string escapedPath, ref int totalAnnotations, int maxTotalAnnotations)
{
    foreach ((int LineNumber, int Covered, int Total) gap in GapReportUtils.GetBranchGaps(file.Lines).Take(10))
    {
        if (totalAnnotations >= maxTotalAnnotations) return;

        string title = GitHubWorkflowFormatter.EscapeProperty("Partial Branch Coverage");
        string message = GitHubWorkflowFormatter.EscapeMessage($"[{relativePath} : {gap.LineNumber}] {gap.Covered} / {gap.Total} branches covered.");
        Console.WriteLine($"::warning file={escapedPath},line={gap.LineNumber},title={title}::{message}");
        totalAnnotations++;
    }
}

static bool ShouldShowBreakdown(double lineCoverage, double branchCoverage)
{
    return lineCoverage < 1.0 || (branchCoverage < 1.0 && !double.IsNaN(branchCoverage));
}

static string FormatMetricRow(string label, double value, double threshold, bool failOnNaN = false)
{
    bool passed = double.IsNaN(value) ? !failOnNaN : value >= threshold;
    string status = passed ? "✅" : "❌";
    string display = double.IsNaN(value) ? "N/A" : value.ToString("P2", CultureInfo.InvariantCulture);
    return $"| **{label}** | {display} | {threshold.ToString("P2", CultureInfo.InvariantCulture)} | {status} |";
}

static string GetFileBreakdown(Coverage coverage)
{
    StringBuilder sb = new();
    sb.AppendLine();
    sb.AppendLine("| File | Line Coverage | Branch Coverage | Gaps |");
    sb.AppendLine("| :--- | :---: | :---: | :--- |");

    var lowestFiles = coverage.Files
                              .Select(f => new
                              {
                                  f.Path,
                                  Line = f.CalculateFileCoverage(),
                                  Branch = f.CalculateFileCoverage(CoverageType.Branch),
                                  f.Lines
                              })
                              .Where(f => !double.IsNaN(f.Line) && (f.Line < 1.0 || (f.Branch < 1.0 && !double.IsNaN(f.Branch))))
                              .OrderBy(f => double.IsNaN(f.Branch) ? f.Line : Math.Min(f.Line, f.Branch))
                              .Take(10);

    foreach (var item in lowestFiles)
    {
        string lineDisplay = double.IsNaN(item.Line) ? "N/A" : item.Line.ToString("P2", CultureInfo.InvariantCulture);
        string branchDisplay = double.IsNaN(item.Branch) ? "N/A" : item.Branch.ToString("P2", CultureInfo.InvariantCulture);
        string escapedPath = EscapeMarkdown(item.Path);
        string gapDisplay = FormatGapDisplay(item.Lines);

        sb.AppendLine(CultureInfo.InvariantCulture, $"| `{escapedPath}` | {lineDisplay} | {branchDisplay} | {gapDisplay} |");
    }

    return sb.ToString();
}

static string FormatGapDisplay(IEnumerable<LineCoverage> lines)
{
    LineCoverage[] linesAsArray = lines.ToArray();

    List<string> gaps = [];
    string lineGaps = GapReportUtils.GetLineGaps(linesAsArray);
    if (!string.IsNullOrEmpty(lineGaps))
    {
        gaps.Add($"Lines: {lineGaps}");
    }

    List<(int LineNumber, int Covered, int Total)> branchGaps = GapReportUtils.GetBranchGaps(linesAsArray).Take(5).ToList();
    if (branchGaps.Count > 0)
    {
        gaps.Add($"Branches: {string.Join(", ", branchGaps.Select(b => $"L{b.LineNumber} ({b.Covered}/{b.Total})"))}");
    }

    return string.Join("<br/>", gaps);
}

static string EscapeMarkdown(string text)
{
    return text.Replace("|", "\\|").Replace("`", "\\` ");
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

internal sealed partial class Program
{
    private Program()
    {
    }

    [GeneratedRegex(@"\{output\}([\\/][^\s&|;<>""']*)")]
    private static partial Regex OutputPathSuffixRegex();
}
