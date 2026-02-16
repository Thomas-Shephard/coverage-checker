using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Services;

internal class CoverageRegressionService : ICoverageRegressionService
{
    private sealed record CoverageStrategy(
        CoverageType Type,
        Func<LineStats, int> GetCovered,
        Func<LineStats, int> GetTotal,
        Func<LineStats, bool> IsApplicable
    );

    private static readonly CoverageStrategy[] Strategies =
    [
        new(CoverageType.Line, s => s.IsCovered ? 1 : 0, _ => 1, _ => true),
        new(CoverageType.Branch, s => s.CoveredBranches ?? 0, s => s.Branches ?? 0, s => s.Branches.HasValue)
    ];

    public RegressionResult CheckRegression(Coverage baseline, Coverage current, IDictionary<string, string>? renames = null, double epsilon = CoverageAnalyser.DefaultEpsilon)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);

        // Aggregate current coverage stats by path to handle files split across packages.
        Dictionary<string, FileStats> currentStats = AggregateStats(current.Files);

        // Aggregate baseline coverage stats by path, applying renames to the lookup key.
        Dictionary<string, FileStats> baselineStats = AggregateStats(baseline.Files, renames);

        List<RegressedFile> regressedFiles = [];
        foreach (KeyValuePair<string, FileStats> baselinePair in baselineStats)
        {
            string path = baselinePair.Key;
            if (!currentStats.TryGetValue(path, out FileStats? currentFileStats))
            {
                continue; // File deleted, not a regression.
            }

            FileStats baselineFileStats = baselinePair.Value;
            foreach (CoverageStrategy strategy in Strategies)
            {
                double baselineCoverage = baselineFileStats.Calculate(strategy);
                double currentCoverage = currentFileStats.Calculate(strategy);

                if (double.IsNaN(baselineCoverage) || double.IsNaN(currentCoverage))
                    continue;

                if (currentCoverage < baselineCoverage - epsilon)
                {
                    regressedFiles.Add(new RegressedFile(
                        path,
                        currentFileStats.PackageName,
                        baselineCoverage,
                        currentCoverage,
                        baselineCoverage - currentCoverage,
                        strategy.Type
                    ));
                }
            }
        }

        return new RegressionResult(regressedFiles);
    }

    private static Dictionary<string, FileStats> AggregateStats(IEnumerable<FileCoverage> files, IDictionary<string, string>? renames = null)
    {
        Dictionary<string, FileStats> statsByPath = new(PathUtils.PathComparer);
        foreach (FileCoverage file in files)
        {
            string lookupPath = (renames != null && renames.TryGetValue(file.Path, out string? newPath)) ? newPath : file.Path;
            if (!statsByPath.TryGetValue(lookupPath, out FileStats? stats))
            {
                stats = new FileStats(file.PackageName);
                statsByPath[lookupPath] = stats;
            }

            foreach (LineCoverage line in file.Lines)
            {
                stats.AddOrMergeLine(line);
            }
        }

        return statsByPath;
    }

    private sealed class FileStats(string? packageName)
    {
        public string? PackageName { get; } = packageName;

        // Using a dictionary to merge lines by line number correctly.
        private readonly Dictionary<int, LineStats> _lines = [];

        public void AddOrMergeLine(LineCoverage line)
        {
            if (_lines.TryGetValue(line.LineNumber, out LineStats? existing))
            {
                existing.IsCovered |= line.IsCovered;
                if (!line.Branches.HasValue)
                    return;

                if (existing.Branches.HasValue && existing.Branches != line.Branches)
                {
                    throw new CoverageParseException($"Cannot merge line {line.LineNumber} due to a branches mismatch");
                }

                existing.Branches ??= line.Branches;
                existing.CoveredBranches = Math.Max(existing.CoveredBranches ?? 0, line.CoveredBranches ?? 0);
            }
            else
            {
                _lines[line.LineNumber] = new LineStats
                {
                    IsCovered = line.IsCovered,
                    Branches = line.Branches,
                    CoveredBranches = line.CoveredBranches
                };
            }
        }

        public double Calculate(CoverageStrategy strategy)
        {
            int covered = 0, total = 0;
            foreach (LineStats line in _lines.Values.Where(line => strategy.IsApplicable(line)))
            {
                covered += strategy.GetCovered(line);
                total += strategy.GetTotal(line);
            }

            return total == 0 ? double.NaN : (double)covered / total;
        }
    }

    private sealed class LineStats
    {
        public bool IsCovered { get; set; }
        public int? Branches { get; set; }
        public int? CoveredBranches { get; set; }
    }
}
