using System.Xml;

namespace CoverageChecker.Parsers;

internal abstract class BaseParser {
    internal void ParseCoverageFromFilePath(string filePath) {
        try {
            XmlReaderSettings settings = new() {
                Async = true,
                IgnoreWhitespace = true
            };

            using XmlReader reader = XmlReader.Create(filePath, settings);
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