using System.Globalization;
using System.Text;
using CoverageChecker.Results;
using CoverageChecker.Utils;
using Microsoft.Extensions.Logging;

namespace CoverageChecker.CommandLine;

internal static class GitHubReportWriter
{
    public static async Task WriteSummary(CoverageResult result, CommandLineOptions options, ILogger logger)
    {
        string? summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (string.IsNullOrEmpty(summaryPath)) return;

        try
        {
            string summary = BuildSummary(result, options);
            await File.AppendAllTextAsync(summaryPath, summary);

            if (IsAnyThresholdViolated(result, options))
            {
                Coverage coverageToAnnotate = (options.Delta && result is { HasDeltaChangedLines: true, DeltaCoverage: not null })
                    ? result.DeltaCoverage
                    : result.OverallCoverage;

                string rootDirectory = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? options.Directory;
                rootDirectory = PathUtils.GetNormalizedFullPath(rootDirectory);

                EmitAnnotations(coverageToAnnotate, rootDirectory);
            }
        }
        catch (Exception ex)
        {
            logger.LogGitHubSummaryWriteFailed(ex, summaryPath);
        }
    }

    private static string BuildSummary(CoverageResult result, CommandLineOptions options)
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

    private static void AppendDeltaSummary(StringBuilder summary, CoverageResult result, CommandLineOptions options)
    {
        if (result.HasDeltaChangedLines)
        {
            summary.AppendLine(FormatMetricRow("Delta Line Coverage", result.DeltaLineCoverage, options.EffectiveDeltaLineThreshold, failOnNaN: true));
            summary.AppendLine(FormatMetricRow("Delta Branch Coverage", result.DeltaBranchCoverage, options.EffectiveDeltaBranchThreshold));
        }
        else if (options.StrictDelta && result.ChangedFilesMissingCoverage.Count > 0)
        {
            summary.AppendLine("| **Delta Coverage** | N/A (Changed files missing from coverage data) | - | ❌ |");
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

    private static bool IsAnyThresholdViolated(CoverageResult result, CommandLineOptions options)
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
               (double.IsNaN(result.DeltaLineCoverage) ||
                options.EffectiveDeltaLineThreshold > result.DeltaLineCoverage ||
                options.EffectiveDeltaBranchThreshold > result.DeltaBranchCoverage);
    }

    private static void EmitAnnotations(Coverage coverage, string rootDirectory)
    {
        int totalAnnotations = 0;
        const int maxTotalAnnotations = 50;

        foreach (FileCoverage file in coverage.Files.OrderBy(f => f.CalculateFileCoverage()))
        {
            if (totalAnnotations >= maxTotalAnnotations) break;

            EmitFileAnnotations(file, rootDirectory, ref totalAnnotations, maxTotalAnnotations);
        }
    }

    private static void EmitFileAnnotations(FileCoverage file, string rootDirectory, ref int totalAnnotations, int maxTotalAnnotations)
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

    private static void EmitLineGaps(FileCoverage file, string relativePath, string escapedPath, ref int totalAnnotations, int maxTotalAnnotations)
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

    private static void EmitBranchGaps(FileCoverage file, string relativePath, string escapedPath, ref int totalAnnotations, int maxTotalAnnotations)
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

    private static bool ShouldShowBreakdown(double lineCoverage, double branchCoverage)
    {
        return lineCoverage < 1.0 || (branchCoverage < 1.0 && !double.IsNaN(branchCoverage));
    }

    private static string FormatMetricRow(string label, double value, double threshold, bool failOnNaN = false)
    {
        bool passed = double.IsNaN(value) ? !failOnNaN : value >= threshold;
        string status = passed ? "✅" : "❌";
        string display = double.IsNaN(value) ? "N/A" : value.ToString("P2", CultureInfo.InvariantCulture);
        return $"| **{label}** | {display} | {threshold.ToString("P2", CultureInfo.InvariantCulture)} | {status} |";
    }

    private static string GetFileBreakdown(Coverage coverage)
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

    private static string FormatGapDisplay(IEnumerable<LineCoverage> lines)
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

    private static string EscapeMarkdown(string text)
    {
        return text.Replace("|", "\\|").Replace("`", " ");
    }
}
