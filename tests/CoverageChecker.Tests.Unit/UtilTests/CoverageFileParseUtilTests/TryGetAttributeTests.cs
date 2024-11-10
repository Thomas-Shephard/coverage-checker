using System.Xml;
using CoverageChecker.Utils;

namespace CoverageChecker.Tests.Unit.UtilTests.CoverageFileParseUtilTests;

public class TryGetAttributeTests
{
    [Test]
    public void CoverageFileParseUtils_TryGetAttribute_StringAttributeFound_ReturnsValue()
    {
        const string attributeValue = "value";
        const string xml = $"""<{XmlReaderTestUtils.ElementName} {XmlReaderTestUtils.AttributeName}="{attributeValue}"/>""";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        bool attributeFound = reader.TryGetAttribute(XmlReaderTestUtils.AttributeName, out string? attribute);

        Assert.Multiple(() =>
        {
            Assert.That(attributeFound, Is.True);
            Assert.That(attribute, Is.EqualTo(attributeValue));
        });
    }

    [Test]
    public void CoverageFileParseUtils_TryGetAttribute_StringAttributeNotFound_ReturnsDefault()
    {
        const string xml = $"<{XmlReaderTestUtils.ElementName}/>";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        bool attributeFound = reader.TryGetAttribute(XmlReaderTestUtils.AttributeName, out string? attribute);

        Assert.Multiple(() =>
        {
            Assert.That(attributeFound, Is.False);
            Assert.That(attribute, Is.Default);
        });
    }

    [Test]
    public void CoverageFileParseUtils_TryGetAttribute_IntAttributeFound_ReturnsValue()
    {
        const string attributeValue = "42";
        const string xml = $"""<{XmlReaderTestUtils.ElementName} {XmlReaderTestUtils.AttributeName}="{attributeValue}"/>""";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        bool attributeFound = reader.TryGetAttribute(XmlReaderTestUtils.AttributeName, out int attribute);

        Assert.Multiple(() =>
        {
            Assert.That(attributeFound, Is.True);
            Assert.That(attribute, Is.EqualTo(int.Parse(attributeValue)));
        });
    }

    [Test]
    public void CoverageFileParseUtils_TryGetAttribute_IntAttributeNotFound_ReturnsDefault()
    {
        const string xml = $"<{XmlReaderTestUtils.ElementName}/>";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        bool attributeFound = reader.TryGetAttribute(XmlReaderTestUtils.AttributeName, out int attribute);

        Assert.Multiple(() =>
        {
            Assert.That(attributeFound, Is.False);
            Assert.That(attribute, Is.Default);
        });
    }

    [Test]
    public void CoverageFileParseUtils_TryGetAttribute_IntAttributeFoundButInvalid_ThrowsException()
    {
        const string attributeValue = "not-a-number";
        const string xml = $"""<{XmlReaderTestUtils.ElementName} {XmlReaderTestUtils.AttributeName}="{attributeValue}"/>""";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        Exception e = Assert.Throws<CoverageParseException>(() => reader.TryGetAttribute<int>(XmlReaderTestUtils.AttributeName, out _));
        Assert.That(e.Message, Is.EqualTo($"Failed to parse attribute '{XmlReaderTestUtils.AttributeName}' on node '{XmlReaderTestUtils.ElementName}'"));
    }
}