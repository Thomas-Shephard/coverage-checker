using System.Xml.Linq;
using CoverageChecker.Results;
using CoverageChecker.Utils;
using Microsoft.Extensions.FileSystemGlobbing;

namespace CoverageChecker.Parsers;

public abstract class BaseParser(string directory, IEnumerable<string> globPatterns, bool failIfNoFilesFound) {
    private readonly Matcher _matcher = new Matcher().AddFromGlobPatterns(globPatterns);

    public Coverage LoadCoverage() {
        string[] filePaths = _matcher.GetResultsInFullPath(directory)
                                     .ToArray();

        if (filePaths.Length is 0 && failIfNoFilesFound)
            throw new CoverageParseException("No coverage files found");

        Coverage coverage = new();

        foreach (string filePath in filePaths) {
            try {
                LoadCoverage(coverage, XDocument.Load(filePath));
            } catch(Exception e) {
                // Rethrow the exception if it is a CoverageException otherwise throw a generic failed to load exception
                if (e is CoverageException) throw;
                throw new CoverageParseException("Failed to load coverage file");
            }
        }

        return coverage;
    }

    protected abstract void LoadCoverage(Coverage coverage, XDocument coverageDocument);
}