using CoverageChecker.Results;

namespace CoverageChecker.Services;

internal class DeltaCoverageService(ICoverageMergeService mergeService) : IDeltaCoverageService
{
    public DeltaResult FilterCoverage(Coverage coverage, IDictionary<string, HashSet<int>> changedLines)
    {
        Coverage resultCoverage = new();
        bool hasChangedLines = false;

        ILookup<string, FileCoverage> coverageFilesByPath = coverage.Files.ToLookup(f => f.Path, StringComparer.OrdinalIgnoreCase);

        foreach ((string gitPath, HashSet<int> changedLineNumbers) in changedLines)
        {
            if (!coverageFilesByPath.Contains(gitPath)) continue;

            FileCoverage mergedFile = resultCoverage.GetOrCreateFile(gitPath);

            foreach (FileCoverage fileCoverage in coverageFilesByPath[gitPath])
            {
                IEnumerable<LineCoverage> filteredLines = fileCoverage.Lines.Where(line => changedLineNumbers.Contains(line.LineNumber));

                foreach (LineCoverage line in filteredLines)
                {
                    hasChangedLines = true;
                    mergedFile.AddOrMergeLine(line.Clone(), mergeService);
                }
            }
        }

        return new DeltaResult(resultCoverage, hasChangedLines);
    }
}