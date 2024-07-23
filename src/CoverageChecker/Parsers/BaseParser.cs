using System.Xml;
using System.Xml.Linq;
using CoverageChecker.Results;
using CoverageChecker.Utils;
using Microsoft.Extensions.FileSystemGlobbing;

namespace CoverageChecker.Parsers;

internal abstract class BaseParser(string directory, IEnumerable<string> globPatterns, bool failIfNoFilesFound) {
    private readonly Matcher _matcher = new Matcher().AddFromGlobPatterns(globPatterns);

    internal async Task <Coverage> LoadCoverage() {
        string[] filePaths = _matcher.GetResultsInFullPath(directory)
                                     .ToArray();

        if (filePaths.Length is 0 && failIfNoFilesFound)
            throw new CoverageParseException("No coverage files found");

        Coverage coverage = new();

        foreach (string filePath in filePaths) {
            try {
                XmlReaderSettings settings = new() {
                    Async = true,
                    IgnoreWhitespace = true
                };

                using XmlReader reader = XmlReader.Create(filePath, settings);
                await LoadCoverage(coverage, reader);
            } catch (Exception e) when (e is not CoverageException) {
                throw new CoverageParseException("Failed to load coverage file");
            }
        }

        return coverage;
    }

    protected abstract Task LoadCoverage(Coverage coverage, XmlReader reader);
}