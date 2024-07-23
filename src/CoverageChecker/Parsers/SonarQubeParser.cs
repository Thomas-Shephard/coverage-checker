using System.Xml;
using System.Xml.Linq;
using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Parsers;

internal class SonarQubeParser(string directory, IEnumerable<string> globPatterns, bool failIfNoFilesFound = true) : BaseParser(directory, globPatterns, failIfNoFilesFound) {
    internal SonarQubeParser(string directory, string globPattern, bool failIfNoFilesFound = true) : this(directory, [globPattern], failIfNoFilesFound) { }

    protected override async Task LoadCoverage(Coverage coverage, XmlReader reader) {
        // Check that the root element is 'coverage' and read the 'version' attribute
        // Check root element is 'coverage' but DO NOT consume the element
        await reader.ReadAsync();

        string version = reader.GetRequiredAttribute("version");

        if (version is not "1")
            throw new CoverageParseException("Unsupported version of the SonarQube coverage report");

        await reader.ReadElementsAndParse("file", async () => {
            await LoadFileCoverage(coverage, reader);
        });
    }

    private static async Task LoadFileCoverage(Coverage coverage, XmlReader reader) {
        string filePath = reader.GetRequiredAttribute("path");

        FileCoverage file = coverage.GetOrCreateFile(filePath);

        await reader.ReadElementsAndParse("lineToCover", () => {
            LoadLineCoverage(file, reader);
            return Task.CompletedTask;
        });
    }

    private static void LoadLineCoverage(FileCoverage file, XmlReader reader) {
        int lineNumber = int.Parse(reader.GetRequiredAttribute("lineNumber"));
        bool isCovered = bool.Parse(reader.GetRequiredAttribute("covered"));

        int? branches = reader.GetAttribute("branchesToCover") is not null ? int.Parse(reader.GetRequiredAttribute("branchesToCover")) : null;
        int? coveredBranches = reader.GetAttribute("coveredBranches") is not null ? int.Parse(reader.GetRequiredAttribute("coveredBranches")) : null;

        file.AddLine(lineNumber, isCovered, branches, coveredBranches);
    }
}