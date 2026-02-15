using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Services;

internal class CoverageRegressionService : ICoverageRegressionService
{
    private static readonly CoverageType[] CoverageTypes = Enum.GetValues<CoverageType>();

    public RegressionResult CheckRegression(Coverage baseline, Coverage current, IDictionary<string, string>? renames = null, double epsilon = CoverageAnalyser.DefaultEpsilon)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);

        // Aggregate current coverage stats by path to handle files split across packages.
        Dictionary<string, FileStats> currentStats = CoverageRegressionService.AggregateStats(current.Files);

        // Aggregate baseline coverage stats by path, applying renames to the lookup key.
        Dictionary<string, FileStats> baselineStats = CoverageRegressionService.AggregateStats(baseline.Files, renames);

        List<RegressedFile> regressedFiles = [];
        foreach (KeyValuePair<string, FileStats> baselinePair in baselineStats)
        {
            string path = baselinePair.Key;
            if (!currentStats.TryGetValue(path, out FileStats? currentFileStats))
            {
                continue; // File deleted, not a regression.
            }

            FileStats baselineFileStats = baselinePair.Value;
            foreach (CoverageType type in CoverageTypes)
            {
                double baselineCoverage = baselineFileStats.Calculate(type);
                double currentCoverage = currentFileStats.Calculate(type);

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
                        type
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
                // If existing has no branches, take them from incoming.
                // If both have branches, they should match (mergeService would throw if they didn't).
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

        public double Calculate(CoverageType type)
        {
            int covered = 0, total = 0;
            foreach (LineStats line in _lines.Values)
            {
                if (type == CoverageType.Line)
                {
                    covered += line.IsCovered ? 1 : 0;
                    total++;
                }
                else if (type == CoverageType.Branch && line.Branches.HasValue)
                {
                    covered += line.CoveredBranches ?? 0;
                    total += line.Branches.Value;
                }
            }

            return total == 0 ? double.NaN : (double)covered / total;
        }

        private sealed class LineStats
        {
            public bool IsCovered { get; set; }
            public int? Branches { get; set; }
            public int? CoveredBranches { get; set; }
        }
    }
}
