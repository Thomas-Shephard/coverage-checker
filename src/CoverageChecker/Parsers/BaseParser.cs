using System.Xml;

namespace CoverageChecker.Parsers;

internal abstract class BaseParser {
    internal static readonly XmlReaderSettings XmlReaderSettings = new() {
        IgnoreComments = true,
        IgnoreWhitespace = true
    };

    internal void ParseCoverageFromFilePath(string filePath) {
        try {
            using XmlReader reader = XmlReader.Create(filePath, XmlReaderSettings);
            ParseCoverageFromXmlReader(reader);
        } catch (Exception e) when (e is not CoverageException) {
            throw new CoverageParseException("Failed to load coverage file");
        }
    }

    internal void ParseCoverageFromXmlReader(XmlReader reader) {
        LoadCoverage(reader);
    }

    protected abstract void LoadCoverage(XmlReader reader);
}