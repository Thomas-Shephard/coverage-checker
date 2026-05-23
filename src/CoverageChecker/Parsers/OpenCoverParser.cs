using System.Xml;
using CoverageChecker.Results;
using CoverageChecker.Services;
using CoverageChecker.Utils;
using Microsoft.Extensions.Logging;

namespace CoverageChecker.Parsers;

internal partial class OpenCoverParser(Coverage coverage, ILogger<OpenCoverParser> logger, ICoverageMergeService coverageMergeService) : ParserBase(logger)
{
    private readonly Dictionary<string, Dictionary<int, OpenCoverLine>> _lines = new(PathUtils.PathComparer);
    private readonly Dictionary<string, Dictionary<int, int>> _knownBranches = new(PathUtils.PathComparer);
    private readonly Dictionary<string, Dictionary<int, LineMetadata>> _knownMetadata = new(PathUtils.PathComparer);

    protected override void LoadCoverage(XmlReader reader)
    {
        _lines.Clear();

        if (!reader.ReadToFollowing("CoverageSession") || reader.Depth != 0)
            throw new CoverageParseException("Expected CoverageSession to be the root element");

        reader.TryEnterElement("CoverageSession", () =>
        {
            int depth = reader.Depth;
            while (reader.Depth == depth)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    reader.Read();
                    continue;
                }

                if (reader.Name == "Modules")
                {
                    reader.TryEnterElement("Modules", () =>
                    {
                        reader.ParseElements("Module", () =>
                        {
                            LoadModuleCoverage(reader);
                        });
                    });
                }
                else
                {
                    reader.ConsumeElement(reader.Name);
                }
            }
        });

        FlushLines();
    }

    private void LoadModuleCoverage(XmlReader reader)
    {
        Dictionary<int, string> files = [];

        reader.TryEnterElement("Module", () =>
        {
            int depth = reader.Depth;
            while (reader.Depth == depth)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    reader.Read();
                    continue;
                }

                switch (reader.Name)
                {
                    case "Files":
                        reader.TryEnterElement("Files", () =>
                        {
                            reader.ParseElements("File", () =>
                            {
                                int uid = reader.GetRequiredAttribute<int>("uid");
                                string fullPath = ResolveFullPath(reader.GetRequiredAttribute<string>("fullPath"));
                                files[uid] = fullPath;
                                reader.ConsumeElement("File");
                            });
                        });
                        break;
                    case "Classes":
                        reader.TryEnterElement("Classes", () =>
                        {
                            reader.ParseElements("Class", () =>
                            {
                                LoadClassCoverage(reader, files);
                            });
                        });
                        break;
                    default:
                        reader.ConsumeElement(reader.Name);
                        break;
                }
            }
        });
    }

    private void LoadClassCoverage(XmlReader reader, IReadOnlyDictionary<int, string> files)
    {
        string? className = null;

        reader.TryEnterElement("Class", () =>
        {
            int depth = reader.Depth;
            while (reader.Depth == depth)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    reader.Read();
                    continue;
                }

                switch (reader.Name)
                {
                    case "FullName":
                        className = reader.ReadElementContentAsString().Trim();
                        break;
                    case "Methods":
                        reader.TryEnterElement("Methods", () =>
                        {
                            reader.ParseElements("Method", () =>
                            {
                                LoadMethodCoverage(reader, files, className);
                            });
                        });
                        break;
                    default:
                        reader.ConsumeElement(reader.Name);
                        break;
                }
            }
        });
    }

    private void LoadMethodCoverage(XmlReader reader, IReadOnlyDictionary<int, string> files, string? className)
    {
        string? methodName = null;
        string? methodSignature = null;
        int? methodFileId = null;
        List<OpenCoverSequencePoint> sequencePoints = [];

        reader.TryEnterElement("Method", () =>
        {
            int depth = reader.Depth;
            while (reader.Depth == depth)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    reader.Read();
                    continue;
                }

                switch (reader.Name)
                {
                    case "Name":
                        methodName = reader.ReadElementContentAsString().Trim();
                        methodSignature = methodName;
                        break;
                    case "FileRef":
                        methodFileId = reader.GetRequiredAttribute<int>("uid");
                        if (!files.ContainsKey(methodFileId.Value))
                            throw new CoverageParseException($"OpenCover file id '{methodFileId}' was not found");
                        reader.ConsumeElement("FileRef");
                        break;
                    case "SequencePoints":
                        reader.TryEnterElement("SequencePoints", () =>
                        {
                            reader.ParseElements("SequencePoint", () =>
                            {
                                LoadSequencePoint(reader, files, methodFileId, sequencePoints, className, methodName, methodSignature);
                            });
                        });
                        break;
                    case "BranchPoints":
                        reader.TryEnterElement("BranchPoints", () =>
                        {
                            reader.ParseElements("BranchPoint", () =>
                            {
                                AddBranchPoint(reader, files, methodFileId, sequencePoints, className, methodName, methodSignature);
                            });
                        });
                        break;
                    default:
                        reader.ConsumeElement(reader.Name);
                        break;
                }
            }
        });
    }

    private void LoadSequencePoint(XmlReader reader, IReadOnlyDictionary<int, string> files, int? methodFileId, ICollection<OpenCoverSequencePoint> sequencePoints, string? className, string? methodName, string? methodSignature)
    {
        int fileId = GetPointFileId(reader, methodFileId);
        int lineNumber = reader.GetRequiredAttribute<int>("sl");
        bool isCovered = reader.GetRequiredAttribute<int>("vc") > 0;
        reader.TryGetAttribute("offset", out int offset);
        reader.TryGetAttribute("bec", out int branchesToCover);
        reader.TryGetAttribute("bev", out int coveredBranches);

        reader.ConsumeElement("SequencePoint");

        OpenCoverLine line = GetLine(files, fileId, lineNumber);
        line.IsCovered |= isCovered;
        if (!line.HasBranchPoints && branchesToCover > 0)
        {
            line.Branches = Math.Max(line.Branches, branchesToCover);
            line.CoveredBranches = Math.Max(line.CoveredBranches, coveredBranches);
        }

        line.ClassName ??= className;
        line.MethodName ??= methodName;
        line.MethodSignature ??= methodSignature;

        sequencePoints.Add(new OpenCoverSequencePoint(fileId, lineNumber, offset));
    }

    private void AddBranchPoint(XmlReader reader, IReadOnlyDictionary<int, string> files, int? methodFileId, IReadOnlyCollection<OpenCoverSequencePoint> sequencePoints, string? className, string? methodName, string? methodSignature)
    {
        OpenCoverPointLocation? location = GetBranchPointLocation(reader, methodFileId, sequencePoints);
        bool isCovered = reader.GetRequiredAttribute<int>("vc") > 0;

        reader.ConsumeElement("BranchPoint");

        if (location is null)
            return;

        if (!files.ContainsKey(location.FileId))
            throw new CoverageParseException($"OpenCover file id '{location.FileId}' was not found");

        OpenCoverLine line = GetLine(files, location.FileId, location.LineNumber);
        if (!line.HasBranchPoints)
        {
            line.Branches = 0;
            line.CoveredBranches = 0;
            line.HasBranchPoints = true;
        }

        line.Branches++;
        if (isCovered)
        {
            line.CoveredBranches++;
            line.IsCovered = true;
        }

        line.ClassName ??= className;
        line.MethodName ??= methodName;
        line.MethodSignature ??= methodSignature;
    }

    private static int GetPointFileId(XmlReader reader, int? methodFileId)
    {
        if (reader.TryGetAttribute("fileid", out int fileId) && fileId != 0)
            return fileId;

        if (methodFileId is not null)
            return methodFileId.Value;

        throw new CoverageParseException($"Attribute 'fileid' not found on element '{reader.Name}' and no method FileRef was available");
    }

    private static OpenCoverPointLocation? GetBranchPointLocation(XmlReader reader, int? methodFileId, IReadOnlyCollection<OpenCoverSequencePoint> sequencePoints)
    {
        bool hasFileId = reader.TryGetAttribute("fileid", out int fileId) && fileId != 0;
        bool hasLineNumber = reader.TryGetAttribute("sl", out int lineNumber) && lineNumber != 0;

        if (hasFileId && hasLineNumber)
            return new OpenCoverPointLocation(fileId, lineNumber);

        int offset = reader.GetRequiredAttribute<int>("offset");
        OpenCoverSequencePoint? parent = sequencePoints
            .Where(sequencePoint => sequencePoint.Offset <= offset)
            .OrderByDescending(sequencePoint => sequencePoint.Offset)
            .FirstOrDefault();

        if (parent is null && !hasLineNumber)
            return null;

        int resolvedFileId = hasFileId ? fileId : parent?.FileId ?? methodFileId ?? 0;
        if (resolvedFileId == 0)
            throw new CoverageParseException($"Attribute 'fileid' not found on element '{reader.Name}' and no method FileRef was available");

        return new OpenCoverPointLocation(resolvedFileId, hasLineNumber ? lineNumber : parent!.LineNumber);
    }

    private OpenCoverLine GetLine(IReadOnlyDictionary<int, string> files, int fileId, int lineNumber)
    {
        if (!files.TryGetValue(fileId, out string? filePath))
            throw new CoverageParseException($"OpenCover file id '{fileId}' was not found");

        LogProcessingFile(filePath);
        Dictionary<int, OpenCoverLine> fileLines = GetOrCreateLineMap(_lines, filePath);
        if (!fileLines.TryGetValue(lineNumber, out OpenCoverLine? line))
        {
            line = new OpenCoverLine(filePath, lineNumber);
            fileLines[lineNumber] = line;
        }

        return line;
    }

    private void FlushLines()
    {
        foreach (OpenCoverLine line in _lines.Values.SelectMany(fileLines => fileLines.Values))
        {
            FileCoverage file = coverage.GetOrCreateFile(line.FilePath);

            int branches = line.Branches;
            int coveredBranches = line.CoveredBranches;
            if (branches > 0)
            {
                GetOrCreateLineMap(_knownBranches, line.FilePath)[line.LineNumber] = branches;
            }
            else if (_knownBranches.TryGetValue(line.FilePath, out Dictionary<int, int>? fileBranches) &&
                     fileBranches.TryGetValue(line.LineNumber, out int knownBranches))
            {
                branches = knownBranches;
                coveredBranches = 0;
            }

            LineMetadata metadata = GetStableMetadata(line);
            LineCoverage lineCoverage = branches > 0
                ? new LineCoverage(line.LineNumber, line.IsCovered, branches, coveredBranches, metadata.ClassName, metadata.MethodName, metadata.MethodSignature)
                : new LineCoverage(line.LineNumber, line.IsCovered, className: metadata.ClassName, methodName: metadata.MethodName, methodSignature: metadata.MethodSignature);

            file.AddOrMergeLine(lineCoverage, coverageMergeService);
        }
    }

    private LineMetadata GetStableMetadata(OpenCoverLine line)
    {
        LineMetadata metadata = new(line.ClassName, line.MethodName, line.MethodSignature);
        Dictionary<int, LineMetadata> fileMetadata = GetOrCreateLineMap(_knownMetadata, line.FilePath);
        if (!fileMetadata.TryGetValue(line.LineNumber, out LineMetadata? known))
        {
            fileMetadata[line.LineNumber] = metadata;
            return metadata;
        }

        return known == metadata ? metadata : new LineMetadata(null, null, null);
    }

    private static Dictionary<int, TValue> GetOrCreateLineMap<TValue>(Dictionary<string, Dictionary<int, TValue>> maps, string filePath)
    {
        if (!maps.TryGetValue(filePath, out Dictionary<int, TValue>? lineMap))
        {
            lineMap = [];
            maps[filePath] = lineMap;
        }

        return lineMap;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Processing file: {FilePath}")]
    private partial void LogProcessingFile(string filePath);

    private sealed class OpenCoverLine(string filePath, int lineNumber)
    {
        public string FilePath { get; } = filePath;
        public int LineNumber { get; } = lineNumber;
        public bool IsCovered { get; set; }
        public int Branches { get; set; }
        public int CoveredBranches { get; set; }
        public bool HasBranchPoints { get; set; }
        public string? ClassName { get; set; }
        public string? MethodName { get; set; }
        public string? MethodSignature { get; set; }
    }

    private sealed record OpenCoverSequencePoint(int FileId, int LineNumber, int Offset);

    private sealed record OpenCoverPointLocation(int FileId, int LineNumber);

    private sealed record LineMetadata(string? ClassName, string? MethodName, string? MethodSignature);
}
