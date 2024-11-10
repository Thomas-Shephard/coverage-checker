using System.Xml;

namespace CoverageChecker.Parsers;

internal abstract class ParserBase
{
    internal static readonly XmlReaderSettings XmlReaderSettings = new()
    {
        IgnoreComments = true,
        IgnoreWhitespace = true,
        DtdProcessing = DtdProcessing.Ignore
    };

    internal void ParseCoverage(string filePath)
    {
        using XmlReader reader = XmlReader.Create(filePath, XmlReaderSettings);
        ParseCoverage(reader);
    }

    private void ParseCoverage(XmlReader reader)
    {
        try
        {
            LoadCoverage(reader);
        }
        catch (Exception exception) when (exception is not CoverageException)
        {
            throw new CoverageParseException("Failed to load coverage file", exception);
        }
    }

    protected abstract void LoadCoverage(XmlReader reader);
}