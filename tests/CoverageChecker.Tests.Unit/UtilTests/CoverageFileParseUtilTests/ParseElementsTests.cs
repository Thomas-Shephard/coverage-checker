using System.Xml;
using CoverageChecker.Utils;

namespace CoverageChecker.Tests.Unit.UtilTests.CoverageFileParseUtilTests;

public class ParseElementsTests {
    [Test]
    public void CoverageFileParseUtils_ParseElements_EmptyElementsFound_ParsesElements() {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName} index="1"/>
                                <{XmlReaderTestUtils.ChildElementName} index="2"/>
                                <{XmlReaderTestUtils.ChildElementName} index="3"/>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ChildElementName, XmlNodeType.Element);

        int childCount = 0;

        reader.ParseElements(XmlReaderTestUtils.ChildElementName, () => {
            childCount++;

            Assert.That(reader.GetAttribute("index"), Is.EqualTo(childCount.ToString()));

            reader.ConsumeElement(XmlReaderTestUtils.ChildElementName);
        });

        Assert.That(childCount, Is.EqualTo(3));
    }

    [Test]
    public void CoverageFileParseUtils_ParseElements_ElementsFound1_ParsesElements() {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName} index="1"></{XmlReaderTestUtils.ChildElementName}>
                                <{XmlReaderTestUtils.ChildElementName} index="2"></{XmlReaderTestUtils.ChildElementName}>
                                <{XmlReaderTestUtils.ChildElementName} index="3"></{XmlReaderTestUtils.ChildElementName}>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ChildElementName, XmlNodeType.Element);

        int childCount = 0;

        reader.ParseElements(XmlReaderTestUtils.ChildElementName, () => {
            childCount++;

            Assert.That(reader.GetAttribute("index"), Is.EqualTo(childCount.ToString()));

            reader.ConsumeElement(XmlReaderTestUtils.ChildElementName);
        });

        Assert.That(childCount, Is.EqualTo(3));
    }

    [Test]
    public void CoverageFileParseUtils_ParseElements_ElementsFound2_ParsesElements() {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName} index="1"/>
                                <{XmlReaderTestUtils.ChildElementName} index="2"></{XmlReaderTestUtils.ChildElementName}>
                                <{XmlReaderTestUtils.ChildElementName} index="3">
                                    <{XmlReaderTestUtils.GrandchildElementName}/>
                                </{XmlReaderTestUtils.ChildElementName}>
                                <{XmlReaderTestUtils.ChildElementName} index="4"/>
                                <{XmlReaderTestUtils.ChildElementName} index="5">
                                    <{XmlReaderTestUtils.GrandchildElementName}>
                                        <{XmlReaderTestUtils.GreatGrandchildElementName}/>
                                    </{XmlReaderTestUtils.GrandchildElementName}>
                                </{XmlReaderTestUtils.ChildElementName}>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ChildElementName, XmlNodeType.Element);

        int childCount = 0;

        reader.ParseElements(XmlReaderTestUtils.ChildElementName, () => {
            childCount++;

            Assert.That(reader.GetAttribute("index"), Is.EqualTo(childCount.ToString()));

            reader.ConsumeElement(XmlReaderTestUtils.ChildElementName);
        });

        Assert.That(childCount, Is.EqualTo(5));
    }

    [Test]
    public void CoverageFileParseUtils_ParseElements_UnexpectedElementsFound1_ParsesElements() {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName} index="1"></{XmlReaderTestUtils.ChildElementName}>
                                <{XmlReaderTestUtils.UnknownElementName}></{XmlReaderTestUtils.UnknownElementName}>
                                <{XmlReaderTestUtils.ChildElementName} index="2"></{XmlReaderTestUtils.ChildElementName}>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ChildElementName, XmlNodeType.Element);

        int childCount = 0;

        reader.ParseElements(XmlReaderTestUtils.ChildElementName, () => {
            childCount++;

            Assert.That(reader.GetAttribute("index"), Is.EqualTo(childCount.ToString()));

            reader.ConsumeElement(XmlReaderTestUtils.ChildElementName);
        });

        Assert.That(childCount, Is.EqualTo(2));
    }

    [Test]
    public void CoverageFileParseUtils_ParseElements_UnexpectedElementsFound2_ParsesElements() {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.UnknownElementName}></{XmlReaderTestUtils.UnknownElementName}>
                                <{XmlReaderTestUtils.UnknownElementName}></{XmlReaderTestUtils.UnknownElementName}>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.UnknownElementName, XmlNodeType.Element);

        reader.ParseElements(XmlReaderTestUtils.ChildElementName, () => {
            Assert.Fail("Incorrect element has been parsed");
        });
    }

    [Test]
    public void CoverageFileParseUtils_ParseElements_StartsAtUnexpectedElementType_ThrowsCoverageParseException() {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName} index="1"></{XmlReaderTestUtils.ChildElementName}>
                                <{XmlReaderTestUtils.ChildElementName} index="2"></{XmlReaderTestUtils.ChildElementName}>
                                <{XmlReaderTestUtils.ChildElementName} index="3"></{XmlReaderTestUtils.ChildElementName}>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ChildElementName, XmlNodeType.EndElement);

        Exception e = Assert.Throws<CoverageParseException>(() => reader.ParseElements(XmlReaderTestUtils.ChildElementName, () => { }));
        Assert.That(e.Message, Is.EqualTo($"Expected to find start element but found '{XmlReaderTestUtils.ChildElementName}' of type '{XmlNodeType.EndElement}'"));
    }
}