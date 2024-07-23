using System.Xml.Linq;
using CoverageChecker.Results;
using CoverageChecker.Utils;
using Microsoft.Extensions.FileSystemGlobbing;

namespace CoverageChecker.Parsers;

public abstract class BaseParser(string directory, IEnumerable<string> globPatterns, bool failIfNoFilesFound) {
    public Coverage LoadCoverage() {
        string[] filePaths = GlobUtils.GetFilePathsFromGlobPatterns(directory, globPatterns).ToArray();

        if (filePaths.Length is 0 && failIfNoFilesFound)
            throw new CoverageParseException("No coverage files found");

        Coverage coverage = new();

        foreach (string filePath in filePaths) {
            try {
                LoadCoverage(coverage, XDocument.Load(filePath));
            } catch (Exception e) when (e is not CoverageException) {
                throw new CoverageParseException("Failed to load coverage file");
            }
        }

        return coverage;
    }

    protected abstract void LoadCoverage(Coverage coverage, XDocument coverageDocument);
}