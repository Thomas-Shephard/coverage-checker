using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
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

static async Task<int> Run(CommandLineOptions options)
{
    bool isGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";

    using ILoggerFactory loggerFactory = CreateLoggerFactory(isGitHubActions);
    ILogger logger = loggerFactory.CreateLogger("CoverageChecker.CommandLine");

    CoverageAnalyserOptions analyserOptions = new()
    {
        CoverageFormat = options.CoverageFormat,
        Directory = options.Directory,
        GlobPatterns = options.GlobPatterns,
        Include = options.Include,
        Exclude = options.Exclude,
        RenameThreshold = options.RenameThreshold
    };

    CoverageAnalyser coverageAnalyser = new(analyserOptions, loggerFactory);

    if (!TryAnalyseCoverage(coverageAnalyser, logger, out Coverage? coverage))
    {
        return 1;
    }

    CoverageResult result = new(
        coverage,
        coverage.CalculateOverallCoverage(),
        coverage.CalculateOverallCoverage(CoverageType.Branch),
        null,
        double.NaN,
        double.NaN,
        false
    );

    logger.LogLineCoverage(result.LineCoverage);
    logger.LogBranchCoverage(result.BranchCoverage);

    if (options.Delta)
    {
        if (!TryAnalyseDeltaCoverage(coverageAnalyser, coverage, options, logger, out Coverage? deltaCoverage, out bool hasDeltaChangedLines))
        {
            return 1;
        }

        if (hasDeltaChangedLines && deltaCoverage != null)
        {
            result = result with
            {
                DeltaCoverage = deltaCoverage,
                HasDeltaChangedLines = true,
                DeltaLineCoverage = deltaCoverage.CalculateOverallCoverage(),
                DeltaBranchCoverage = deltaCoverage.CalculateOverallCoverage(CoverageType.Branch)
            };

            logger.LogDeltaLineCoverage(result.DeltaLineCoverage);
            logger.LogDeltaBranchCoverage(result.DeltaBranchCoverage);
        }
        else
        {
            logger.LogNoDeltaLinesFound();
        }
    }

    if (isGitHubActions)
    {
        await WriteGitHubSummary(result, options, logger);
    }

    return CheckThresholds(result, options, logger, isGitHubActions);
}

static async Task<int> RunCommandAndCheck(RunOptions options)
{
    string? tempDir = null;
    string outputDir = options.Output ?? (tempDir = Path.Combine(Path.GetTempPath(), "coverage-checker", Guid.NewGuid().ToString()));

    if (!Directory.Exists(outputDir))
    {
        Directory.CreateDirectory(outputDir);
    }

    bool isGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
    using ILoggerFactory loggerFactory = CreateLoggerFactory(isGitHubActions);
    ILogger logger = loggerFactory.CreateLogger("CoverageChecker.CommandLine");

    try
    {
        // Safely replace {output} placeholder. We should ensure the directory is quoted to avoid shell injection or path issues, unless already quoted.
        string escapedOutputDir = outputDir;
        if (!options.Command.Contains("\"{output}\"") && !options.Command.Contains("'{output}'"))
        {
            escapedOutputDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? $"\"{outputDir}\""
                : $"'{outputDir.Replace("'", "'\\''")}'";
        }

        string command = options.Command.Replace("{output}", escapedOutputDir);
        logger.LogRunningCommand(command);

        using Process process = new();
        process.StartInfo.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "sh";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            process.StartInfo.ArgumentList.Add("/c");
            process.StartInfo.ArgumentList.Add(command);
        }
        else
        {
            process.StartInfo.ArgumentList.Add("-c");
            process.StartInfo.ArgumentList.Add(command);
        }
        
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        // Do not redirect to allow inheriting the parent console's stdout/stderr (real-time output)
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.RedirectStandardError = false;

        process.Start();
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(options.Timeout));
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            logger.LogCommandFailed(1);
            return 1;
        }

        int exitCode = process.ExitCode;

        if (exitCode != 0)
        {
            logger.LogCommandFailed(exitCode);
            return exitCode;
        }

        CommandLineOptions effectiveOptions = options with { Directory = outputDir };
        return await Run(effectiveOptions);
    }
    catch (Exception ex)
    {
        logger.LogCriticalError(ex);
        return 1;
    }
    finally
    {
        if (!string.IsNullOrEmpty(tempDir) && Directory.Exists(tempDir))
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}

static bool TryAnalyseDeltaCoverage(CoverageAnalyser analyser, Coverage coverage, CommandLineOptions options, ILogger logger, out Coverage? deltaCoverage, out bool hasChangedLines)
{
    deltaCoverage = null;
    hasChangedLines = false;
    try
    {
        DeltaResult deltaResult = analyser.AnalyseDeltaCoverage(options.DeltaBase, coverage);
        deltaCoverage = deltaResult.Coverage;
        hasChangedLines = deltaResult.HasChangedLines;
        return true;
    }
    catch (Exception ex) when (ex is GitException or ArgumentException)
    {
        logger.LogDeltaAnalysisFailed(ex);
        return false;
    }
}

static int CheckThresholds(CoverageResult result, CommandLineOptions options, ILogger logger, bool isGitHubActions)
{
    bool failed = EvaluateOverallThresholds(result, options, logger);

    if (options.Delta && result is { HasDeltaChangedLines: true, DeltaCoverage: not null })
    {
        failed |= EvaluateDeltaThresholds(result, options, logger);
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

static bool EvaluateOverallThresholds(CoverageResult result, CommandLineOptions options, ILogger logger)
{
    bool failed = false;
    if (options.LineThreshold > result.LineCoverage)
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

    if (!double.IsNaN(result.DeltaLineCoverage) && options.LineThreshold > result.DeltaLineCoverage)
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
    summary.AppendLine(FormatMetricRow("Line Coverage", result.LineCoverage, options.LineThreshold));
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
        summary.AppendLine(FormatMetricRow("Delta Line Coverage", result.DeltaLineCoverage, options.LineThreshold));
        summary.AppendLine(FormatMetricRow("Delta Branch Coverage", result.DeltaBranchCoverage, options.BranchThreshold));
    }
    else
    {
        summary.AppendLine("| Delta Coverage | N/A (No changed lines) | - | ✅ |");
    }
}

static bool IsAnyThresholdViolated(CoverageResult result, CommandLineOptions options)
{
    if (options.LineThreshold > result.LineCoverage || options.BranchThreshold > result.BranchCoverage)
    {
        return true;
    }

    return options.Delta && result.HasDeltaChangedLines &&
           (options.LineThreshold > result.DeltaLineCoverage || options.BranchThreshold > result.DeltaBranchCoverage);
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

static string FormatMetricRow(string label, double value, double threshold)
{
    bool passed = double.IsNaN(value) || value >= threshold;
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
