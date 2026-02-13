using System.Xml;
using System.Globalization;
using CoverageChecker.Utils;

namespace CoverageChecker.Tests.Unit.UtilTests.CoverageFileParseUtilTests;

public class TryGetAttributeTests
{
    [Test]
    public void CoverageFileParseUtilsTryGetAttributeStringAttributeFoundReturnsValue()
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
    public void CoverageFileParseUtilsTryGetAttributeStringAttributeNotFoundReturnsDefault()
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
    public void CoverageFileParseUtilsTryGetAttributeIntAttributeFoundReturnsValue()
    {
        const string attributeValue = "42";
        const string xml = $"""<{XmlReaderTestUtils.ElementName} {XmlReaderTestUtils.AttributeName}="{attributeValue}"/>""";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        bool attributeFound = reader.TryGetAttribute(XmlReaderTestUtils.AttributeName, out int attribute);

        Assert.Multiple(() =>
        {
            Assert.That(attributeFound, Is.True);
            Assert.That(attribute, Is.EqualTo(int.Parse(attributeValue, CultureInfo.InvariantCulture)));
        });
    }

    [Test]
    public void CoverageFileParseUtilsTryGetAttributeIntAttributeNotFoundReturnsDefault()
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
    public void CoverageFileParseUtilsTryGetAttributeIntAttributeFoundButInvalidThrowsException()
    {
        const string attributeValue = "not-a-number";
        const string xml = $"""<{XmlReaderTestUtils.ElementName} {XmlReaderTestUtils.AttributeName}="{attributeValue}"/>""";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        Exception e = Assert.Throws<CoverageParseException>(() => reader.TryGetAttribute<int>(XmlReaderTestUtils.AttributeName, out _));
        Assert.That(e.Message, Is.EqualTo($"Failed to parse attribute '{XmlReaderTestUtils.AttributeName}' on node '{XmlReaderTestUtils.ElementName}'"));
    }
}