using System.Xml;
using CoverageChecker.Utils;

namespace CoverageChecker.UnitTests.UtilTests.CoverageFileParseUtilTests;

public class TryEnterElementTests {
    private static readonly XmlReaderSettings XmlReaderSettings = new() {
        Async = true,
        IgnoreWhitespace = true
    };

    [Test]
    public void CoverageFileParseUtils_TryEnterElement_ElementFound_ReturnsTrue() {
        const string xml = """
                           <element>
                               <child/>
                           </element>
                           """;

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);
        IXmlLineInfo lineInfo = reader as IXmlLineInfo ?? throw new Exception("This reader does not support line info");

        bool enteredElement = reader.TryEnterElement("element", () => {
            Assert.Multiple(() => {
                Assert.That(reader.NodeType, Is.EqualTo(XmlNodeType.Element));
                Assert.That(reader.Name, Is.EqualTo("child"));
                Assert.That(lineInfo.LineNumber, Is.EqualTo(2));
                Assert.That(lineInfo.LinePosition, Is.EqualTo(6));
            });
        });

        Assert.Multiple(() => {
            Assert.That(enteredElement, Is.True);
            Assert.That(lineInfo.LineNumber, Is.EqualTo(3));
            Assert.That(lineInfo.LinePosition, Is.EqualTo(11));
        });
    }

    [Test]
    public void CoverageFileParseUtils_TryEnterElement_SelfClosingElementFound_ReturnsFalse() {
        const string xml = "<element/>";

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);
        IXmlLineInfo lineInfo = reader as IXmlLineInfo ?? throw new Exception("This reader does not support line info");

        reader.TryEnterElement("element", () => {
            Assert.Fail("Should not enter element");
        });

        Assert.Multiple(() => {
            Assert.That(lineInfo.LineNumber, Is.EqualTo(1));
            Assert.That(lineInfo.LinePosition, Is.EqualTo(11));
        });
    }

    [Test]
    public void CoverageFileParseUtils_TryEnterElement_ElementNotFound_ThrowsCoverageParseException() {
        const string xml = "<element/>";

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);

        Exception e = Assert.Throws<CoverageParseException>(() => reader.TryEnterElement("child", () => { }));
        Assert.That(e.Message, Is.EqualTo("Element 'child' not found"));
    }

    [Test]
    public void CoverageFileParseUtils_TryEnterElement_ElementNotFound_DoesNotThrowCoverageParseException() {
        const string xml = "<element/>";

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);

        bool enteredElement = reader.TryEnterElement("child", () => { }, throwIfNotFound: false);
        Assert.That(enteredElement, Is.False);
    }

    [Test]
    public void CoverageFileParseUtils_TryEnterElement_MultipleElementsFound() {
        const string xml = """
                           <element>
                               <child-a></child-a>
                               <child-b></child-b>
                           </element>
                           """;

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);

        reader.TryEnterElement("element", () => {
            bool enteredChildA = reader.TryEnterElement("child-a", () => {

            });

            bool enteredChildB = reader.TryEnterElement("child-b", () => {

            });

            Assert.Multiple(() => {
                Assert.That(enteredChildA, Is.True);
                Assert.That(enteredChildB, Is.True);
            });
        });
    }


    [Test]
    public void CoverageFileParseUtils_TryEnterElement_MultipleElementsFound_Success() {
        const string xml = """
                           <element>
                               <child>
                                   <grandchild/>
                               </child>
                           </element>
                           """;

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);
        IXmlLineInfo lineInfo = reader as IXmlLineInfo ?? throw new Exception("This reader does not support line info");

        bool enteredElement = reader.TryEnterElement("element", () => {
            Assert.Multiple(() => {
                Assert.That(lineInfo.LineNumber, Is.EqualTo(2));
                Assert.That(lineInfo.LinePosition, Is.EqualTo(6));
            });

            bool enteredChild = reader.TryEnterElement("child", () => {
                Assert.Multiple(() => {
                    Assert.That(lineInfo.LineNumber, Is.EqualTo(3));
                    Assert.That(lineInfo.LinePosition, Is.EqualTo(10));
                });

                bool enteredGrandchild = reader.TryEnterElement("grandchild", () => { });
                Assert.That(enteredGrandchild, Is.False);

                Assert.Multiple(() => {
                    Assert.That(lineInfo.LineNumber, Is.EqualTo(4));
                    Assert.That(lineInfo.LinePosition, Is.EqualTo(7));
                });
            });
            Assert.That(enteredChild, Is.True);

            Assert.Multiple(() => {
                Assert.That(lineInfo.LineNumber, Is.EqualTo(5));
                Assert.That(lineInfo.LinePosition, Is.EqualTo(3));
            });
        });

        Assert.That(enteredElement, Is.True);

        Assert.Multiple(() => {
            Assert.That(lineInfo.LineNumber, Is.EqualTo(5));
            Assert.That(lineInfo.LinePosition, Is.EqualTo(11));
        });
    }

    [Test]
    public void CoverageFileParseUtils_TryEnterElement_MultipleElementsFound_Success2() {
        const string xml = """
                           <element>
                               <child/>
                           </element>
                           """;

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);
        IXmlLineInfo lineInfo = reader as IXmlLineInfo ?? throw new Exception("This reader does not support line info");

        bool enteredElement = reader.TryEnterElement("element", () => {
            Assert.Multiple(() => {
                Assert.That(lineInfo.LineNumber, Is.EqualTo(2));
                Assert.That(lineInfo.LinePosition, Is.EqualTo(6));
            });

            bool enteredChild = reader.TryEnterElement("child", () => { });
            Assert.That(enteredChild, Is.False);

            Assert.Multiple(() => {
                Assert.That(lineInfo.LineNumber, Is.EqualTo(3));
                Assert.That(lineInfo.LinePosition, Is.EqualTo(3));
            });
        });

        Assert.That(enteredElement, Is.True);

        Assert.Multiple(() => {
            Assert.That(lineInfo.LineNumber, Is.EqualTo(3));
            Assert.That(lineInfo.LinePosition, Is.EqualTo(11));
        });
    }

    [Test]
    public void CoverageFileParseUtils_TryEnterParsedElement() {
        const string xml = """
                           <element>
                               <child index="1"/>
                               <child index="2"/>
                           </element>
                           """;

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);
        IXmlLineInfo lineInfo = reader as IXmlLineInfo ?? throw new Exception("This reader does not support line info");

        reader.TryEnterElement("element", () => {
            int childCount = 0;

            reader.ParseElements("child", () => {
                childCount++;

                Assert.Multiple(() => {
                    Assert.That(reader.NodeType, Is.EqualTo(XmlNodeType.Element));
                    Assert.That(reader.Name, Is.EqualTo("child"));
                    Assert.That(reader.GetAttribute("index"), Is.EqualTo(childCount.ToString()));
                    Assert.That(lineInfo.LineNumber, Is.EqualTo(childCount + 1));
                    Assert.That(lineInfo.LinePosition, Is.EqualTo(6));
                });

                reader.TryEnterElement("child", () => { Assert.Fail("Should not enter child"); });
            });

            Assert.That(childCount, Is.EqualTo(2));
        });
    }

}