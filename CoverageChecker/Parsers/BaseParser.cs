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

        FileCoverage[] fileCoverages = filePaths.Select(filePath => {
                                                    try {
                                                        return XDocument.Load(filePath);
                                                    } catch (Exception) {
                                                        throw new CoverageParseException("Failed to load coverage file");
                                                    }
                                                })
                                                .SelectMany(LoadCoverageFile)
                                                .ToArray();

        return new Coverage(fileCoverages);
    }

    protected abstract FileCoverage[] LoadCoverageFile(XDocument coverageFile);
}