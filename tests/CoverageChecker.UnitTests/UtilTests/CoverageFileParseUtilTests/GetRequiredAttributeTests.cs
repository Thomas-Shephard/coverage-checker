using System.Xml;
using CoverageChecker.Utils;

namespace CoverageChecker.UnitTests.UtilTests.CoverageFileParseUtilTests;

public class GetRequiredAttributeTests {
    private static readonly XmlReaderSettings XmlReaderSettings = new() {
        Async = true,
        IgnoreWhitespace = true
    };

    [Test]
    public void CoverageFileParseUtils_GetRequiredAttribute_StringAttributeFound_ReturnsValue() {
        const string xml = """<element attribute="value"/>""";

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);
        reader.Read();

        string attribute = reader.GetRequiredAttribute<string>("attribute");

        Assert.That(attribute, Is.EqualTo("value"));
    }

    [Test]
    public void CoverageFileParseUtils_GetRequiredAttribute_StringAttributeNotFound_ThrowsCoverageParseException() {
        const string xml = "<element/>";

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);
        reader.Read();

        Exception e = Assert.Throws<CoverageParseException>(() => reader.GetRequiredAttribute<string>("attribute"));
        Assert.That(e.Message, Is.EqualTo("Attribute 'attribute' not found on element 'element'"));
    }
}