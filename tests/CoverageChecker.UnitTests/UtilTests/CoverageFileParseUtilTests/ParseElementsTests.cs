using System.Xml;
using CoverageChecker.Utils;

namespace CoverageChecker.UnitTests.UtilTests.CoverageFileParseUtilTests;

public class ParseElementsTests {
    private static readonly XmlReaderSettings XmlReaderSettings = new() {
        Async = true,
        IgnoreWhitespace = true
    };

    [Test]
    public void CoverageFileParseUtils_ParseElements_ElementNotFound() {
        const string xml = """
                           <element>
                               <child/>
                               <unexpected/>
                               <child/>
                               <unexpected/>
                               <unexpected/>
                               <child/>
                           </element>
                           """;

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);

        reader.TryEnterElement("element", () => {
            int childCount = 0;

            reader.ParseElements("child", () => {
                childCount++;
                reader.ConsumeElement("child");
            });

            Assert.That(childCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void CoverageFileParseUtils_ParseElements_ElementFound() {
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

                reader.ConsumeElement("child");
            });

            Assert.That(childCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void CoverageFileParseUtils_ParseElements_ParseMissingChild_ElementFound() {
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

                reader.ConsumeElement("child");
            });

            Assert.That(childCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void CoverageFileParseUtils_ParseElements2_ElementsFound() {
        const string xml = """
                           <element>
                               <child index="1"/>
                               <child index="2"></child>
                               <child index="3">
                                   <grandchild/>
                               </child>
                               <child index="4"/>
                           </element>
                           """;

        XmlReader reader = XmlReader.Create(new StringReader(xml), XmlReaderSettings);

        reader.TryEnterElement("element", () => {
            int childCount = 0;

            reader.ParseElements("child", () => {
                childCount++;

                Assert.Multiple(() => {
                    Assert.That(reader.NodeType, Is.EqualTo(XmlNodeType.Element));
                    Assert.That(reader.Name, Is.EqualTo("child"));
                    Assert.That(reader.GetAttribute("index"), Is.EqualTo(childCount.ToString()));
                });

                reader.ConsumeElement("child");
            });

            Assert.That(childCount, Is.EqualTo(4));
        });
    }
}