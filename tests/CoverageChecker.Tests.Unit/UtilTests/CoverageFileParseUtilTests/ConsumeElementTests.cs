using System.Xml;
using CoverageChecker.Utils;

namespace CoverageChecker.Tests.Unit.UtilTests.CoverageFileParseUtilTests;

public class ConsumeElementTests
{
    [Test]
    public void CoverageFileParseUtilsConsumeElementElementFoundConsumesElement()
    {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName}/>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        reader.ConsumeElement(XmlReaderTestUtils.ElementName);

        lineInfo.CheckPosition(3, 11);
    }

    [Test]
    public void CoverageFileParseUtilsConsumeElementEmptyElementFoundConsumesElement()
    {
        const string xml = $"<{XmlReaderTestUtils.ElementName}/>";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        reader.ConsumeElement(XmlReaderTestUtils.ElementName);

        lineInfo.CheckPosition(1, 11);
    }

    [Test]
    public void CoverageFileParseUtilsConsumeElementStartOfNestedElementConsumesElement()
    {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName}></{XmlReaderTestUtils.ChildElementName}>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.ChildElementName, XmlNodeType.Element);

        reader.ConsumeElement(XmlReaderTestUtils.ChildElementName, 1);

        lineInfo.CheckPosition(3, 3);
    }

    [Test]
    public void CoverageFileParseUtilsConsumeElementInsideOfNestedElement1ConsumesElement()
    {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName}>
                                    <{XmlReaderTestUtils.GrandchildElementName}/>
                                </{XmlReaderTestUtils.ChildElementName}>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.GrandchildElementName, XmlNodeType.Element);

        reader.ConsumeElement(XmlReaderTestUtils.ChildElementName, 1);

        lineInfo.CheckPosition(5, 3);
    }

    [Test]
    public void CoverageFileParseUtilsConsumeElementInsideOfNestedElement2ConsumesElement()
    {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName}>
                                    <{XmlReaderTestUtils.GrandchildElementName}>
                                        <{XmlReaderTestUtils.GreatGrandchildElementName}/>
                                    </{XmlReaderTestUtils.GrandchildElementName}>
                                </{XmlReaderTestUtils.ChildElementName}>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.GrandchildElementName, XmlNodeType.Element);

        reader.ConsumeElement(XmlReaderTestUtils.ChildElementName, 1);

        lineInfo.CheckPosition(7, 3);
    }

    [Test]
    public void CoverageFileParseUtilsConsumeElementInsideOfNestedElement3ConsumesElement()
    {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName}>
                                    <{XmlReaderTestUtils.GrandchildElementName}/>
                                    <{XmlReaderTestUtils.GrandchildElementName}>
                                        <{XmlReaderTestUtils.GreatGrandchildElementName}/>
                                    </{XmlReaderTestUtils.GrandchildElementName}>
                                </{XmlReaderTestUtils.ChildElementName}>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.GrandchildElementName, XmlNodeType.Element);

        reader.ConsumeElement(XmlReaderTestUtils.ChildElementName, 1);

        lineInfo.CheckPosition(8, 3);
    }

    [Test]
    public void CoverageFileParseUtilsConsumeElementInsideOfNestedElement4ConsumesElement()
    {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName}>
                                    <{XmlReaderTestUtils.GrandchildElementName}/>
                                    <{XmlReaderTestUtils.GrandchildElementName}>
                                        <{XmlReaderTestUtils.GreatGrandchildElementName}/>
                                    </{XmlReaderTestUtils.GrandchildElementName}>
                                    <{XmlReaderTestUtils.GrandchildElementName}/>
                                </{XmlReaderTestUtils.ChildElementName}>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.GreatGrandchildElementName, XmlNodeType.Element);

        reader.ConsumeElement(XmlReaderTestUtils.ChildElementName, 1);

        lineInfo.CheckPosition(9, 3);
    }

    [Test]
    public void CoverageFileParseUtilsConsumeElementInsideOfNestedElement5ConsumesElement()
    {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName}/>
                                <{XmlReaderTestUtils.ElementName}/>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.ChildElementName, XmlNodeType.Element);

        reader.ConsumeElement(XmlReaderTestUtils.ElementName, 0);

        lineInfo.CheckPosition(4, 11);
    }

    [Test]
    public void CoverageFileParseUtilsConsumeElementEndOfNestedElementConsumesElement()
    {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName}></{XmlReaderTestUtils.ChildElementName}>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.ChildElementName, XmlNodeType.EndElement);

        reader.ConsumeElement(XmlReaderTestUtils.ChildElementName, 1);

        lineInfo.CheckPosition(3, 3);
    }

    [Test]
    public void CoverageFileParseUtilsConsumeElementEmptyNestedElementConsumesElement()
    {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName}/>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.ChildElementName, XmlNodeType.Element);

        reader.ConsumeElement(XmlReaderTestUtils.ChildElementName, 1);

        lineInfo.CheckPosition(3, 3);
    }

    [Test]
    public void CoverageFileParseUtilsConsumeElementElementNotFoundThrowsCoverageParseException()
    {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName}/>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        Exception e = Assert.Throws<CoverageParseException>(() => reader.ConsumeElement(XmlReaderTestUtils.UnknownElementName));
        Assert.That(e.Message, Is.EqualTo($"Expected to consume element '{XmlReaderTestUtils.UnknownElementName}' but found '{XmlReaderTestUtils.ElementName}'"));
    }

    [Test]
    public void CoverageFileParseUtilsConsumeElementEmptyElementNotFoundThrowsCoverageParseException()
    {
        const string xml = $"<{XmlReaderTestUtils.ElementName}/>";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        Exception e = Assert.Throws<CoverageParseException>(() => reader.ConsumeElement(XmlReaderTestUtils.UnknownElementName));
        Assert.That(e.Message, Is.EqualTo($"Expected to consume element '{XmlReaderTestUtils.UnknownElementName}' but found '{XmlReaderTestUtils.ElementName}'"));
    }

    [Test]
    public void CoverageFileParseUtilsConsumeElementDepthAlreadyEscapedThrowsCoverageParseException()
    {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName}>
                                    <{XmlReaderTestUtils.GrandchildElementName}/>
                                </{XmlReaderTestUtils.ChildElementName}>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ChildElementName, XmlNodeType.EndElement);

        Exception e = Assert.Throws<CoverageParseException>(() => reader.ConsumeElement(XmlReaderTestUtils.GrandchildElementName, 2));
        Assert.That(e.Message, Is.EqualTo($"Expected to consume element '{XmlReaderTestUtils.GrandchildElementName}' but it had already been consumed"));
    }

    [Test]
    public void CoverageFileParseUtilsConsumeElementElementNotEnteredThrowsCoverageParseException()
    {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName}/>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        Exception e = Assert.Throws<CoverageParseException>(() => reader.ConsumeElement(XmlReaderTestUtils.ElementName));
        Assert.That(e.Message, Is.EqualTo($"Expected to consume element '{XmlReaderTestUtils.ElementName}' but it had not been entered"));
    }

    [Test]
    public void CoverageFileParseUtilsConsumeElementEmptyElementNotEnteredThrowsCoverageParseException()
    {
        const string xml = $"<{XmlReaderTestUtils.ElementName}/>";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        Exception e = Assert.Throws<CoverageParseException>(() => reader.ConsumeElement(XmlReaderTestUtils.ElementName));
        Assert.That(e.Message, Is.EqualTo($"Expected to consume element '{XmlReaderTestUtils.ElementName}' but it had not been entered"));
    }
}