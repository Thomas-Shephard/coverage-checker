using System.Xml;
using CoverageChecker.Utils;

namespace CoverageChecker.UnitTests.UtilTests.CoverageFileParseUtilTests;

public class TryGetAttributeTests {
    private static readonly XmlReaderSettings XmlReaderSettings = new() {
        Async = true,
        IgnoreWhitespace = true
    };

    [Test]
    public void CoverageFileParseUtils_TryGetAttribute_StringAttributeFound_ReturnsValue() {
        const string xml = """<element attribute="value"/>""";

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);
        reader.Read();

        bool attributeFound = reader.TryGetAttribute("attribute", out string? attribute);

        Assert.Multiple(() => {
            Assert.That(attributeFound, Is.True);
            Assert.That(attribute, Is.EqualTo("value"));
        });
    }

    [Test]
    public void CoverageFileParseUtils_TryGetAttribute_StringAttributeNotFound_ReturnsDefault() {
        const string xml = "<element/>";

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);
        reader.Read();

        bool attributeFound = reader.TryGetAttribute("attribute", out string? attribute);

        Assert.Multiple(() => {
            Assert.That(attributeFound, Is.False);
            Assert.That(attribute, Is.Default);
        });
    }

    [Test]
    public void CoverageFileParseUtils_TryGetAttribute_IntAttributeFound_ReturnsValue() {
        const string xml = """<element attribute="42"/>""";

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);
        reader.Read();

        bool attributeFound = reader.TryGetAttribute("attribute", out int attribute);

        Assert.Multiple(() => {
            Assert.That(attributeFound, Is.True);
            Assert.That(attribute, Is.EqualTo(42));
        });
    }

    [Test]
    public void CoverageFileParseUtils_TryGetAttribute_IntAttributeNotFound_ReturnsDefault() {
        const string xml = "<element/>";

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);
        reader.Read();

        bool attributeFound = reader.TryGetAttribute("attribute", out int attribute);

        Assert.Multiple(() => {
            Assert.That(attributeFound, Is.False);
            Assert.That(attribute, Is.Default);
        });
    }

    [Test]
    public void CoverageFileParseUtils_TryGetAttribute_IntAttributeFoundButInvalid_ThrowsException() {
        const string xml = """<element attribute="not-a-number"/>""";

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);
        reader.Read();

        Exception e = Assert.Throws<CoverageParseException>(() => reader.TryGetAttribute<int>("attribute", out _));
        Assert.That(e.Message, Is.EqualTo("Failed to parse attribute 'attribute' on node 'element'"));
    }
}