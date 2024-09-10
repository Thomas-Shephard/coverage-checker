using System.Xml;
using CoverageChecker.Utils;

namespace CoverageChecker.Tests.Unit.UtilTests.CoverageFileParseUtilTests;

public class TryEnterElementTests {
    [Test]
    public void CoverageFileParseUtils_TryEnterElement_ElementFound_EntersElement() {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName}/>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        bool enteredElement = reader.TryEnterElement(XmlReaderTestUtils.ElementName, () => {
            lineInfo.CheckPosition(2, 6);
        });

        Assert.That(enteredElement, Is.True);
        lineInfo.CheckPosition(3, 11);
    }

    [Test]
    public void CoverageFileParseUtils_TryEnterElement_NestedElementFound_EntersElement() {
        const string xml = $"""
                            <{XmlReaderTestUtils.ElementName}>
                                <{XmlReaderTestUtils.ChildElementName}>
                                    <{XmlReaderTestUtils.GrandchildElementName}/>
                                </{XmlReaderTestUtils.ChildElementName}>
                            </{XmlReaderTestUtils.ElementName}>
                            """;

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        bool enteredElement = reader.TryEnterElement(XmlReaderTestUtils.ElementName, () => {
            lineInfo.CheckPosition(2, 6);

            bool enteredChildElement = reader.TryEnterElement(XmlReaderTestUtils.ChildElementName, () => {
                lineInfo.CheckPosition(3, 10);
            });

            Assert.That(enteredChildElement, Is.True);
            lineInfo.CheckPosition(5, 3);
        });

        Assert.That(enteredElement, Is.True);
        lineInfo.CheckPosition(5, 11);
    }

    [Test]
    public void CoverageFileParseUtils_TryEnterElement_EmptyElementFound1_EntersElement() {
        const string xml = $"<{XmlReaderTestUtils.ElementName}/>";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        bool enteredElement = reader.TryEnterElement(XmlReaderTestUtils.ElementName, () => {
            Assert.Fail("Empty element should not execute action");
        });

        Assert.That(enteredElement, Is.False);
        lineInfo.CheckPosition(1, 11);
    }

    [Test]
    public void CoverageFileParseUtils_TryEnterElement_EmptyElementFound2_EntersElement() {
        const string xml = $"<{XmlReaderTestUtils.ElementName}></{XmlReaderTestUtils.ElementName}>";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        bool enteredElement = reader.TryEnterElement(XmlReaderTestUtils.ElementName, () => {
            Assert.Fail("Empty element should not execute action");
        });

        Assert.That(enteredElement, Is.False);
        lineInfo.CheckPosition(1, 20);
    }

    [Test]
    public void CoverageFileParseUtils_TryEnterElement_UnknownElementName_ThrowsCoverageParseException() {
        const string xml = $"<{XmlReaderTestUtils.ElementName}/>";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        Exception e = Assert.Throws<CoverageParseException>(() => {
            reader.TryEnterElement(XmlReaderTestUtils.UnknownElementName, () => {
                Assert.Fail("Unknown element name should not execute action");
            });
        });

        Assert.That(e.Message, Is.EqualTo($"Expected to find start element '{XmlReaderTestUtils.UnknownElementName}' but found '{XmlReaderTestUtils.ElementName}' of type '{nameof(XmlNodeType.Element)}'"));
    }

    [Test]
    public void CoverageFileParseUtils_TryEnterElement_ElementEndType_ThrowsCoverageParseException() {
        const string xml = $"<{XmlReaderTestUtils.ElementName}></{XmlReaderTestUtils.ElementName}>";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.EndElement);

        Exception e = Assert.Throws<CoverageParseException>(() => {
            reader.TryEnterElement(XmlReaderTestUtils.ElementName, () => {
                Assert.Fail("Element end type should not execute action");
            });
        });

        Assert.That(e.Message, Is.EqualTo($"Expected to find start element '{XmlReaderTestUtils.ElementName}' but found '{XmlReaderTestUtils.ElementName}' of type '{nameof(XmlNodeType.EndElement)}'"));
    }

    [Test]
    public void CoverageFileParseUtils_TryEnterElement_UnknownElementName_ReturnsFalse() {
        const string xml = $"<{XmlReaderTestUtils.ElementName}/>";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.Element);

        bool entersElement = reader.TryEnterElement(XmlReaderTestUtils.UnknownElementName, () => {
            Assert.Fail("Unknown element name should not execute action");
        }, false);

        Assert.That(entersElement, Is.False);
        lineInfo.CheckPosition(1, 2);
    }

    [Test]
    public void CoverageFileParseUtils_TryEnterElement_ElementEndType_ReturnsFalse() {
        const string xml = $"<{XmlReaderTestUtils.ElementName}></{XmlReaderTestUtils.ElementName}>";

        XmlReader reader = XmlReaderTestUtils.CreateXmlReader(xml);
        IXmlLineInfo lineInfo = XmlReaderTestUtils.GetXmlLineInfo(reader);

        reader.MoveTo(XmlReaderTestUtils.ElementName, XmlNodeType.EndElement);

        bool entersElement = reader.TryEnterElement(XmlReaderTestUtils.ElementName, () => {
            Assert.Fail("Element end type should not execute action");
        }, false);

        Assert.That(entersElement, Is.False);
        lineInfo.CheckPosition(1, 12);
    }
}