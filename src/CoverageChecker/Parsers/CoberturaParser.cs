using System.Xml;
using CoverageChecker.Results;
using CoverageChecker.Services;
using CoverageChecker.Utils;
using Microsoft.Extensions.Logging;

namespace CoverageChecker.Parsers;

internal partial class CoberturaParser(Coverage coverage, ILogger<CoberturaParser> logger, ICoverageMergeService coverageMergeService) : ParserBase(logger)
{
    private readonly Dictionary<string, bool> _fileExistsCache = new(PathUtils.PathComparer);

    protected override void LoadCoverage(XmlReader reader)
    {
        _fileExistsCache.Clear();

        if (!reader.ReadToFollowing("coverage") || reader.Depth != 0)
            throw new CoverageParseException("Expected coverage to be the root element");

        reader.TryEnterElement("coverage", () =>
        {
            string[] sources = GetSources(reader)
                .Select(ResolveFullPath)
                .ToArray();

            reader.TryEnterElement("packages", () =>
            {
                reader.ParseElements("package", () =>
                {
                    LoadPackageCoverage(reader, sources);
                });
            });
        });
    }

    private static List<string> GetSources(XmlReader reader)
    {
        List<string> sources = [];
        reader.TryEnterElement("sources", () =>
        {
            reader.ParseElements("source", () =>
            {
                sources.Add(reader.ReadElementContentAsString().Trim());
            });
        }, false);

        return sources;
    }

    private void LoadPackageCoverage(XmlReader reader, IReadOnlyList<string> sources)
    {
        string packageName = reader.GetRequiredAttribute<string>("name");
        LogProcessingPackage(packageName);

        reader.TryEnterElement("package", () =>
        {
            reader.TryEnterElement("classes", () =>
            {
                reader.ParseElements("class", () =>
                {
                    LoadClassCoverage(reader, packageName, sources);
                });
            });
        });
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Processing package: {PackageName}")]
    private partial void LogProcessingPackage(string packageName);

    private void LoadClassCoverage(XmlReader reader, string packageName, IReadOnlyList<string> sources)
    {
        string filePath = ResolveClassFilePath(reader.GetRequiredAttribute<string>("filename"), sources);
        string className = reader.GetRequiredAttribute<string>("name");

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

    private string ResolveClassFilePath(string filePath, IReadOnlyList<string> sources)
    {
        if (Path.IsPathRooted(filePath) || sources.Count is 0)
        {
            return ResolveFullPath(filePath);
        }

        if (sources.Count is 1)
        {
            return PathUtils.GetNormalizedFullPath(Path.Combine(sources[0], filePath));
        }

        foreach (string source in sources)
        {
            string candidate = PathUtils.GetNormalizedFullPath(Path.Combine(source, filePath));
            if (FileExists(candidate))
            {
                return candidate;
            }
        }

        LogClassFileNotFound(filePath, sources[0]);
        return PathUtils.GetNormalizedFullPath(Path.Combine(sources[0], filePath));
    }

    private bool FileExists(string filePath)
    {
        if (_fileExistsCache.TryGetValue(filePath, out bool exists))
        {
            return exists;
        }

        exists = File.Exists(filePath);
        _fileExistsCache[filePath] = exists;

        return exists;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cobertura class file '{FilePath}' could not be found under any source. Falling back to first source: {Source}")]
    private partial void LogClassFileNotFound(string filePath, string source);

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
