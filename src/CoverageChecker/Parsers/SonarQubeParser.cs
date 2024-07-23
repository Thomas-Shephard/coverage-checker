using System.Xml.Linq;
using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Parsers;

internal class SonarQubeParser(Coverage coverage) : BaseParser {
    protected override void LoadCoverage(XDocument coverageDocument) {
        XElement coverageElement = coverageDocument.GetRequiredElement("coverage");

        // Ensure that the coverage file is valid by checking the version attribute
        if (coverageElement.GetRequiredAttribute("version").Value is not "1")
            throw new CoverageParseException("Attribute 'version' on element 'coverage' must be '1'");

        foreach (XElement fileElement in coverageElement.Elements("file")) {
            LoadFileCoverage(fileElement);
        }
    }

    private void LoadFileCoverage(XElement fileElement) {
        string filePath = fileElement.GetRequiredAttribute("path").Value;

        FileCoverage file = coverage.GetOrCreateFile(filePath);

        foreach (XElement lineToCoverElement in fileElement.Elements("lineToCover")) {
            LoadLineCoverage(file, lineToCoverElement);
        }
    }

    private static void LoadLineCoverage(FileCoverage file, XElement lineToCoverElement) {
        int lineNumber = lineToCoverElement.ParseRequiredAttribute<int>("lineNumber");
        bool isCovered = lineToCoverElement.ParseRequiredAttribute<bool>("covered");

        int? branches = lineToCoverElement.ParseOptionalAttribute<int>("branchesToCover");
        int? coveredBranches = lineToCoverElement.ParseOptionalAttribute<int>("coveredBranches");

        file.AddLine(lineNumber, isCovered, branches, coveredBranches);
    }
}