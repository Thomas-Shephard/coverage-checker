using System.Xml.Linq;
using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Parsers;

public class SonarQubeParser(string directory, IEnumerable<string> globPatterns, bool failIfNoFilesFound = true) : BaseParser(directory, globPatterns, failIfNoFilesFound) {
    public SonarQubeParser(string directory, string globPattern, bool failIfNoFilesFound = true) : this(directory, [globPattern], failIfNoFilesFound) { }

    protected override void LoadCoverage(Coverage coverage, XDocument coverageDocument) {
        XElement coverageElement = coverageDocument.GetRequiredElement("coverage");

        // Ensure that the coverage file is valid by checking the version attribute
        if (coverageElement.GetRequiredAttribute("version").Value is not "1")
            throw new CoverageParseException("Attribute 'version' on element 'coverage' must be '1'");

        foreach (XElement fileElement in coverageElement.Elements("file")) {
            LoadFileCoverage(coverage, fileElement);
        }
    }

    private static void LoadFileCoverage(Coverage coverage, XElement fileElement) {
        string filePath = fileElement.GetRequiredAttribute("path").Value;

        FileCoverage file = coverage.GetOrCreateFile(filePath);

        foreach (XElement lineToCoverElement in fileElement.Elements("lineToCover")) {
            file.AddLine(CreateLineCoverage(lineToCoverElement));
        }
    }

    private static LineCoverage CreateLineCoverage(XElement lineToCoverElement) {
        int lineNumber = lineToCoverElement.ParseRequiredAttribute<int>("lineNumber");
        bool isCovered = lineToCoverElement.ParseRequiredAttribute<bool>("covered");

        int? branches = lineToCoverElement.ParseOptionalAttribute<int>("branchesToCover");
        int? coveredBranches = lineToCoverElement.ParseOptionalAttribute<int>("coveredBranches");

        return new LineCoverage(lineNumber, isCovered, branches, coveredBranches);
    }
}