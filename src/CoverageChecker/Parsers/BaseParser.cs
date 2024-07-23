using System.Xml.Linq;

namespace CoverageChecker.Parsers;

internal abstract class BaseParser {
    internal void ParseCoverageFile(string filePath) {
        try {
            LoadCoverage(XDocument.Load(filePath));
        } catch (Exception e) when (e is not CoverageException) {
            throw new CoverageParseException("Failed to load coverage file");
        }
    }

    protected abstract void LoadCoverage(XDocument coverageDocument);
}