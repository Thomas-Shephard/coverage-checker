using System.Xml;
using CoverageChecker.Results;
using CoverageChecker.Services;
using CoverageChecker.Utils;
using Microsoft.Extensions.Logging;

namespace CoverageChecker.Parsers;

internal partial class CoberturaParser(Coverage coverage, ILogger<CoberturaParser> logger, ICoverageMergeService coverageMergeService) : ParserBase(logger)
{
    protected override void LoadCoverage(XmlReader reader)
    {
        if (!reader.ReadToFollowing("coverage") || reader.Depth != 0)
            throw new CoverageParseException("Expected coverage to be the root element");

        reader.TryEnterElement("coverage", () =>
        {
            string? source = GetSource(reader);

            if (source is not null)
            {
                source = ResolveFullPath(source);
            }

            reader.TryEnterElement("packages", () =>
            {
                reader.ParseElements("package", () =>
                {
                    LoadPackageCoverage(reader, source);
                });
            });
        });
    }

    private static string? GetSource(XmlReader reader)
    {
        string? source = null;
        reader.TryEnterElement("sources", () =>
        {
            reader.ParseElements("source", () =>
            {
                if (source is null)
                {
                    source = reader.ReadElementContentAsString();
                }
                else
                {
                    throw new CoverageParseException("Multiple sources are not supported");
                }
            });
        }, false);

        return source;
    }

    private void LoadPackageCoverage(XmlReader reader, string? source)
    {
        string packageName = reader.GetRequiredAttribute<string>("name");
        LogProcessingPackage(packageName);

        reader.TryEnterElement("package", () =>
        {
            reader.TryEnterElement("classes", () =>
            {
                reader.ParseElements("class", () =>
                {
                    LoadClassCoverage(reader, packageName, source);
                });
            });
        });
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Processing package: {PackageName}")]
    private partial void LogProcessingPackage(string packageName);

    private void LoadClassCoverage(XmlReader reader, string packageName, string? source)
    {
        string filePath = reader.GetRequiredAttribute<string>("filename");
        string className = reader.GetRequiredAttribute<string>("name");

        if (source is not null)
        {
            filePath = Path.Combine(source, filePath);
        }

        filePath = ResolveFullPath(filePath);

        reader.TryEnterElement("class", () =>
        {
            FileCoverage file = coverage.GetOrCreateFile(filePath, packageName);

            reader.TryEnterElement("methods", () =>
            {
                reader.ParseElements("method", () =>
                {
                    LoadMethodCoverage(file, reader, className);
                });
            }, false);

            reader.TryEnterElement("lines", () =>
            {
                reader.ParseElements("line", () =>
                {
                    LoadLineCoverage(file, reader, className);
                });
            }, false);
        });
    }

    private void LoadMethodCoverage(FileCoverage file, XmlReader reader, string className)
    {
        string methodName = reader.GetRequiredAttribute<string>("name");
        reader.TryGetAttribute("signature", out string? methodSignature);

        reader.TryEnterElement("method", () =>
        {
            reader.TryEnterElement("lines", () =>
            {
                reader.ParseElements("line", () =>
                {
                    LoadLineCoverage(file, reader, className, methodName, methodSignature);
                });
            });
        });
    }

    private void LoadLineCoverage(FileCoverage file, XmlReader reader, string className, string? methodName = null, string? methodSignature = null)
    {
        int lineNumber = reader.GetRequiredAttribute<int>("number");
        bool isCovered = reader.GetRequiredAttribute<int>("hits") > 0;
        reader.TryGetAttribute("branch", out bool hasBranchCoverage);

        try
        {
            if (!hasBranchCoverage)
            {
                file.AddOrMergeLine(new LineCoverage(lineNumber, isCovered, className: className, methodName: methodName, methodSignature: methodSignature), coverageMergeService);
                return;
            }

            string conditionCoverage = reader.GetRequiredAttribute<string>("condition-coverage");

            (int branches, int coveredBranches) = ParseConditionCoverage(conditionCoverage);

            file.AddOrMergeLine(new LineCoverage(lineNumber, isCovered, branches, coveredBranches, className, methodName, methodSignature), coverageMergeService);
        }
        finally
        {
            reader.ConsumeElement("line");
        }
    }

    private static (int branches, int coveredBranches) ParseConditionCoverage(string conditionCoverage)
    {
        const string conditionCoverageInvalidMessage = "Attribute 'condition-coverage' on element 'line' is not in the correct format";
        // The condition-coverage attribute is formatted as "x% (y/z)"
        // x is the percentage of branches covered, y is the number of covered branches, and z is the number of branches

        string[] conditionCoverageValues;

        try
        {
            // Retrieve the number of covered branches and the number of branches from the condition-coverage attribute
            conditionCoverageValues = conditionCoverage.Split(" ")[1].TrimStart('(').TrimEnd(')').Split('/');
        }
        catch (IndexOutOfRangeException)
        {
            throw new CoverageParseException(conditionCoverageInvalidMessage);
        }

        // Ensure that only 2 values were found (number of covered branches and number of branches)
        if (conditionCoverageValues.Length != 2)
        {
            throw new CoverageParseException(conditionCoverageInvalidMessage);
        }

        // Ensure that the number of covered branches and the number of branches are integers
        if (!int.TryParse(conditionCoverageValues[0], out int coveredBranches) ||
            !int.TryParse(conditionCoverageValues[1], out int branches))
        {
            throw new CoverageParseException(conditionCoverageInvalidMessage);
        }

        return (branches, coveredBranches);
    }
}