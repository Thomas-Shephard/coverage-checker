using System.Xml;
using System.Xml.Linq;
using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Parsers;

internal class CoberturaParser(string directory, IEnumerable<string> globPatterns, bool failIfNoFilesFound = true) : BaseParser(directory, globPatterns, failIfNoFilesFound) {
    internal CoberturaParser(string directory, string globPattern, bool failIfNoFilesFound = true) : this(directory, [globPattern], failIfNoFilesFound) { }

    protected override async Task LoadCoverage(Coverage coverage, XmlReader reader) {
        reader.ReadStartElement("coverage");

        await reader.ReadNestedElementsAndParse("packages", "package", async () => {
            await LoadPackageCoverage(coverage, reader);
        });
    }

    private static async Task LoadPackageCoverage(Coverage coverage, XmlReader reader) {
        string packageName = reader.GetRequiredAttribute("name");

        await reader.ReadNestedElementsAndParse("classes", "class", async () => {
            await LoadClassCoverage(coverage, reader, packageName);
        });
    }

    private static async Task LoadClassCoverage(Coverage coverage, XmlReader reader, string packageName) {
        string filePath = reader.GetRequiredAttribute("filename");
        string className = reader.GetRequiredAttribute("name");

        FileCoverage file = coverage.GetOrCreateFile(filePath, packageName);

        await reader.ReadNestedElementsAndParse("methods", "method", async () => {
            await LoadMethodCoverage(file, reader, className);
        });

        await reader.ReadNestedElementsAndParse("lines", "line", () => {
            LoadLineCoverage(file, reader, className);
            return Task.CompletedTask;
        });
    }

    private static async Task LoadMethodCoverage(FileCoverage file, XmlReader reader, string className) {
        string methodName = reader.GetRequiredAttribute("name");
        string? methodSignature = reader.GetAttribute("signature");

        await reader.ReadNestedElementsAndParse("lines", "line", () => {
            LoadLineCoverage(file, reader, className, methodName, methodSignature);
            return Task.CompletedTask;
        });
    }

    private static void LoadLineCoverage(FileCoverage file, XmlReader reader, string className, string? methodName = null, string? methodSignature = null) {
        int lineNumber = int.Parse(reader.GetRequiredAttribute("number"));
        bool isCovered = int.Parse(reader.GetRequiredAttribute("hits")) > 0;
        bool hasBranchCoverage = reader.GetAttribute("branch") is not null && bool.Parse(reader.GetRequiredAttribute("branch"));

        if (!hasBranchCoverage) {
            file.AddLine(lineNumber, isCovered, className: className, methodName: methodName, methodSignature: methodSignature);
            return;
        }

        string conditionCoverage = reader.GetRequiredAttribute("condition-coverage");
        (int branches, int coveredBranches) = ParseConditionCoverage(conditionCoverage);

        file.AddLine(lineNumber, isCovered, branches, coveredBranches, className, methodName, methodSignature);
    }

    private static (int branches, int coveredBranches) ParseConditionCoverage(string conditionCoverage) {
        const string conditionCoverageInvalidMessage = "Attribute 'condition-coverage' on element 'line' is not in the correct format";
        // The condition-coverage attribute is formatted as "x% (y/z)"
        // x is the percentage of branches covered, y is the number of covered branches, and z is the number of branches

        string[] conditionCoverageValues;

        try {
            // Retrieve the number of covered branches and the number of branches from the condition-coverage attribute
            conditionCoverageValues = conditionCoverage.Split(" ")[1].TrimStart('(').TrimEnd(')').Split('/');
        } catch (IndexOutOfRangeException) {
            throw new CoverageParseException(conditionCoverageInvalidMessage);
        }

        // Ensure that only 2 values were found (number of covered branches and number of branches)
        if (conditionCoverageValues.Length != 2) {
            throw new CoverageParseException(conditionCoverageInvalidMessage);
        }

        // Ensure that the number of covered branches and the number of branches are integers
        if (!int.TryParse(conditionCoverageValues[0], out int coveredBranches) ||
            !int.TryParse(conditionCoverageValues[1], out int branches)) {
            throw new CoverageParseException(conditionCoverageInvalidMessage);
        }

        return (branches, coveredBranches);
    }
}