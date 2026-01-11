using System.Xml;
using CoverageChecker.Utils;
using Microsoft.Extensions.Logging;

namespace CoverageChecker.Parsers;

internal abstract partial class ParserBase(ILogger logger) : ICoverageParser
{
    internal static readonly XmlReaderSettings XmlReaderSettings = new()
    {
        IgnoreComments = true,
        IgnoreWhitespace = true,
        DtdProcessing = DtdProcessing.Ignore
    };

    private string? RootDirectory { get; set; }

    public void ParseCoverage(string filePath, string? rootDirectory = null)
    {
        RootDirectory = rootDirectory;
        LogOpeningCoverageFile(filePath);
        try
        {
            using XmlReader reader = XmlReader.Create(filePath, XmlReaderSettings);
            LoadCoverage(reader);
        }
        catch (Exception exception) when (exception is not CoverageException)
        {
            throw new CoverageParseException($"Failed to load coverage file: {filePath}", exception);
        }
    }

    protected abstract void LoadCoverage(XmlReader reader);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Opening coverage file: {FilePath}")]
    private partial void LogOpeningCoverageFile(string filePath);

    protected string ResolveFullPath(string path) => PathUtils.GetNormalizedFullPath(path, RootDirectory);
}