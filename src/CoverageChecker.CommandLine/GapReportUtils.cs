using System.Globalization;
using CoverageChecker.Results;

namespace CoverageChecker.CommandLine;

internal static class GapReportUtils
{
    public static string GetLineGaps(IEnumerable<LineCoverage> lines)
    {
        List<(int Start, int End)> ranges = GetLineGapRanges(lines).ToList();
        if (ranges.Count == 0) return string.Empty;

        const int maxGaps = 10;
        IEnumerable<string> displayRanges = ranges.Take(maxGaps).Select(r => FormatRange(r.Start, r.End));
        string result = string.Join(", ", displayRanges);

        if (ranges.Count > maxGaps)
        {
            result += $"... (+{ranges.Count - maxGaps} more)";
        }

        return result;
    }

    public static IEnumerable<(int Start, int End)> GetLineGapRanges(IEnumerable<LineCoverage> lines)
    {
        List<int> uncoveredLines = lines.Where(l => !l.IsCovered)
                                        .Select(l => l.LineNumber)
                                        .OrderBy(l => l)
                                        .ToList();

        if (uncoveredLines.Count == 0)
            yield break;

        int start = uncoveredLines[0];
        int end = start;

        for (int i = 1; i < uncoveredLines.Count; i++)
        {
            if (uncoveredLines[i] == end + 1)
            {
                end = uncoveredLines[i];
            }
            else
            {
                yield return (start, end);
                start = end = uncoveredLines[i];
            }
        }

        yield return (start, end);
    }

    public static IEnumerable<(int LineNumber, int Covered, int Total)> GetBranchGaps(IEnumerable<LineCoverage> lines)
    {
        return GetBranchGapsInternal(lines).OrderBy(l => l.LineNumber);
    }

    private static IEnumerable<(int LineNumber, int Covered, int Total)> GetBranchGapsInternal(IEnumerable<LineCoverage> lines)
    {
        foreach (LineCoverage l in lines)
        {
            if (l is { Branches: { } branches, CoveredBranches: { } covered } && covered < branches)
            {
                yield return (l.LineNumber, covered, branches);
            }
        }
    }

    private static string FormatRange(int start, int end)
    {
        return start == end ? start.ToString(CultureInfo.InvariantCulture) : $"{start}-{end}";
    }
}
