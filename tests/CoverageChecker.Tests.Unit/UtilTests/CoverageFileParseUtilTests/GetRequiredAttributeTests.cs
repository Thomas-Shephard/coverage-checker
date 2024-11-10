using System.Xml;
using CoverageChecker.Utils;

namespace CoverageChecker.Tests.Unit.UtilTests.CoverageFileParseUtilTests;

public class GetRequiredAttributeTests
{
    [Test]
    public void CoverageFileParseUtils_GetRequiredAttribute_StringAttributeFound_ReturnsValue()
    {
        const string attributeValue = "attributeValue";
        const string xml = $"""<{XmlReaderTestUtils.ElementName} {XmlReaderTestUtils.AttributeName}="{attributeValue}"/>""";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        string attribute = reader.GetRequiredAttribute<string>(XmlReaderTestUtils.AttributeName);

        Assert.That(attribute, Is.EqualTo(attributeValue));
    }

    [Test]
    public void CoverageFileParseUtils_GetRequiredAttribute_StringAttributeNotFound_ThrowsCoverageParseException()
    {
        const string xml = $"<{XmlReaderTestUtils.ElementName}/>";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        Exception e = Assert.Throws<CoverageParseException>(() => reader.GetRequiredAttribute<string>(XmlReaderTestUtils.AttributeName));
        Assert.That(e.Message, Is.EqualTo($"Attribute '{XmlReaderTestUtils.AttributeName}' not found on element '{XmlReaderTestUtils.ElementName}'"));
    }
}