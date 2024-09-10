using System.Xml;
using CoverageChecker.Utils;

namespace CoverageChecker.Tests.Unit.UtilTests.CoverageFileParseUtilTests;

public class ConsumeElementTests {
    [Test]
    public void CoverageFileParseUtils_ConsumeElement_ElementFound_ConsumesElement() {
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
    public void CoverageFileParseUtils_ConsumeElement_EmptyElementFound_ConsumesElement() {
        const string xml = $"<{XmlReaderTestUtils.ElementName}/>";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        reader.ConsumeElement(XmlReaderTestUtils.ElementName);

        lineInfo.CheckPosition(1, 11);
    }

    [Test]
    public void CoverageFileParseUtils_ConsumeElement_StartOfNestedElement_ConsumesElement() {
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
    public void CoverageFileParseUtils_ConsumeElement_InsideOfNestedElement1_ConsumesElement() {
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
    public void CoverageFileParseUtils_ConsumeElement_InsideOfNestedElement2_ConsumesElement() {
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
    public void CoverageFileParseUtils_ConsumeElement_InsideOfNestedElement3_ConsumesElement() {
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
    public void CoverageFileParseUtils_ConsumeElement_InsideOfNestedElement4_ConsumesElement() {
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
    public void CoverageFileParseUtils_ConsumeElement_InsideOfNestedElement5_ConsumesElement() {
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
    public void CoverageFileParseUtils_ConsumeElement_EndOfNestedElement_ConsumesElement() {
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
    public void CoverageFileParseUtils_ConsumeElement_EmptyNestedElement_ConsumesElement() {
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
    public void CoverageFileParseUtils_ConsumeElement_ElementNotFound_ThrowsCoverageParseException() {
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
    public void CoverageFileParseUtils_ConsumeElement_EmptyElementNotFound_ThrowsCoverageParseException() {
        const string xml = $"<{XmlReaderTestUtils.ElementName}/>";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        Exception e = Assert.Throws<CoverageParseException>(() => reader.ConsumeElement(XmlReaderTestUtils.UnknownElementName));
        Assert.That(e.Message, Is.EqualTo($"Expected to consume element '{XmlReaderTestUtils.UnknownElementName}' but found '{XmlReaderTestUtils.ElementName}'"));
    }

    [Test]
    public void CoverageFileParseUtils_ConsumeElement_DepthAlreadyEscaped_ThrowsCoverageParseException() {
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
    public void CoverageFileParseUtils_ConsumeElement_ElementNotEntered_ThrowsCoverageParseException() {
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
    public void CoverageFileParseUtils_ConsumeElement_EmptyElementNotEntered_ThrowsCoverageParseException() {
        const string xml = $"<{XmlReaderTestUtils.ElementName}/>";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        Exception e = Assert.Throws<CoverageParseException>(() => reader.ConsumeElement(XmlReaderTestUtils.ElementName));
        Assert.That(e.Message, Is.EqualTo($"Expected to consume element '{XmlReaderTestUtils.ElementName}' but it had not been entered"));
    }
}