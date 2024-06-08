using System.Xml.Linq;
using CoverageChecker.Results;
using CoverageChecker.Utils;
using Microsoft.Extensions.FileSystemGlobbing;

namespace CoverageChecker.Parsers;

public abstract class BaseParser(IEnumerable<string> globPatterns, string? directory = null) {
    // If the directory is not provided, use the current directory
    private readonly string _directory = directory ?? Environment.CurrentDirectory;
    private readonly Matcher _matcher = GlobUtils.CreateFromGlobPatterns(globPatterns);

    public Coverage LoadCoverage() {
        string[] filePaths = _matcher.GetResultsInFullPath(_directory)
                                     .ToArray();

        if (filePaths.Length is 0)
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