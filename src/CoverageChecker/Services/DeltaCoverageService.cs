using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Services;

internal class DeltaCoverageService(ICoverageMergeService mergeService) : IDeltaCoverageService
{
    public DeltaResult FilterCoverage(Coverage coverage, IDictionary<string, HashSet<int>> changedLines)
    {
        Coverage resultCoverage = new();
        int gitChangedLineCount = changedLines.Values.Sum(lines => lines.Count);
        int matchedCoverageLineCount = 0;
        int changedCoverageFileCount = 0;
        List<string> changedFilesMissingCoverage = [];

        ILookup<string, FileCoverage> coverageFilesByPath = coverage.Files.ToLookup(f => f.Path, PathUtils.PathComparer);

        foreach ((string gitPath, HashSet<int> changedLineNumbers) in changedLines)
        {
            if (changedLineNumbers.Count == 0) continue;
            if (!coverageFilesByPath.Contains(gitPath))
            {
                changedFilesMissingCoverage.Add(gitPath);
                continue;
            }

            changedCoverageFileCount++;
            FileCoverage mergedFile = resultCoverage.GetOrCreateFile(gitPath);
            HashSet<int> matchedLineNumbers = [];

            foreach (FileCoverage fileCoverage in coverageFilesByPath[gitPath])
            {
                IEnumerable<LineCoverage> filteredLines = fileCoverage.Lines.Where(line => changedLineNumbers.Contains(line.LineNumber));

                foreach (LineCoverage line in filteredLines)
                {
                    matchedLineNumbers.Add(line.LineNumber);
                    mergedFile.AddOrMergeLine(line.Clone(), mergeService);
                }
            }

            matchedCoverageLineCount += matchedLineNumbers.Count;
        }

        return new DeltaResult(resultCoverage, matchedCoverageLineCount > 0, gitChangedLineCount, matchedCoverageLineCount, changedCoverageFileCount, changedFilesMissingCoverage);
    }
}
