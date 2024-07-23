using CoverageChecker.Parsers;
using CoverageChecker.Results;

namespace CoverageChecker;

public class CoverageAnalyser(CoverageFormat format, string directory, string[] globPatterns, bool failIfNoFilesFound) {
    public async Task<Coverage> AnalyseCoverage() {
        BaseParser parser = format switch {
            CoverageFormat.Cobertura => new CoberturaParser(directory, globPatterns, failIfNoFilesFound),
            CoverageFormat.SonarQube => new SonarQubeParser(directory, globPatterns, failIfNoFilesFound),
            _ => throw new CoverageException("Unsupported coverage format")
        };

        return await parser.LoadCoverage();
    }
}