using System.Xml;
using CoverageChecker.Parsers;

namespace CoverageChecker.Tests.Unit.UtilTests.CoverageFileParseUtilTests;

internal static class XmlReaderTestUtils {
    internal const string AttributeName = "attribute";
    internal const string ElementName = "element";
    internal const string ChildElementName = "child";
    internal const string GrandchildElementName = "grandchild";
    internal const string GreatGrandchildElementName = "great-grandchild";

    internal const string UnknownElementName = "unknown";

    internal static XmlReader CreateXmlReader(string xml) {
        return XmlReader.Create(new StringReader(xml), BaseParser.XmlReaderSettings);
    }

    internal static IXmlLineInfo GetXmlLineInfo(XmlReader reader) {
        if (reader is IXmlLineInfo lineInfo)
            return lineInfo;

        throw new InvalidCastException("This reader does not support line info");
    }

    internal static void MoveTo(this XmlReader reader, string elementName, XmlNodeType nodeType) {
        while (reader.Name != elementName || reader.NodeType != nodeType) {
            if (!reader.Read())
                throw new CoverageParseException($"Could not move to element '{elementName}'");
        }
    }

    internal static void CheckPosition(this IXmlLineInfo lineInfo, int lineNumber, int linePosition) {
        Assert.Multiple(() => {
            Assert.That(lineInfo.LineNumber, Is.EqualTo(lineNumber));
            Assert.That(lineInfo.LinePosition, Is.EqualTo(linePosition));
        });
    }
}