using System.Xml.Linq;
using CoverageChecker.Utils;

namespace CoverageChecker.Tests.UtilTests;

public class CoverageParseUtilTests {
    [Test]
    public void CoverageParseUtils_GetRequiredElement_ElementFound_ReturnsElement() {
        XElement element = new("element");
        XContainer container = new XElement("container", element);

        XElement result = container.GetRequiredElement("element");

        Assert.That(result, Is.EqualTo(element));
    }

    [Test]
    public void CoverageParseUtils_GetRequiredElement_ElementNotFound_ThrowsCoverageParseException() {
        XElement element = new("element");
        XContainer container = new XElement("container", element);

        Assert.Throws<CoverageParseException>(() => container.GetRequiredElement("unknown-element"));
    }

    [Test]
    public void CoverageParseUtils_GetRequiredAttribute_AttributeFound_ReturnsAttribute() {
        XAttribute attribute = new("attribute", "value");
        XElement element = new("element", attribute);

        XAttribute result = element.GetRequiredAttribute("attribute");

        Assert.That(result, Is.EqualTo(attribute));
    }

    [Test]
    public void CoverageParseUtils_GetRequiredAttribute_AttributeNotFound_ThrowsCoverageParseException() {
        XAttribute attribute = new("attribute", "value");
        XElement element = new("element", attribute);

        Assert.Throws<CoverageParseException>(() => element.GetRequiredAttribute("unknown-attribute"));
    }

    [Test]
    public void CoverageParseUtils_ParseOptionalAttribute_AttributeFound_ValidValue_ReturnsValue() {
        XAttribute attribute = new("attribute", "42");
        XElement element = new("element", attribute);

        int? result = element.ParseOptionalAttribute<int>("attribute");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void CoverageParseUtils_ParseOptionalAttribute_AttributeNotFound_ReturnsNull() {
        XAttribute attribute = new("attribute", "42");
        XElement element = new("element", attribute);

        int? result = element.ParseOptionalAttribute<int>("unknown-attribute");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void CoverageParseUtils_ParseOptionalAttribute_AttributeFound_InvalidValue_ThrowsCoverageParseException() {
        XAttribute attribute = new("attribute", "invalid");
        XElement element = new("element", attribute);

        Assert.Throws<CoverageParseException>(() => element.ParseOptionalAttribute<int>("attribute"));
    }

    [Test]
    public void CoverageParseUtils_ParseRequiredAttribute_AttributeFound_ValidValue_ReturnsValue() {
        XAttribute attribute = new("attribute", "42");
        XElement element = new("element", attribute);

        int result = element.ParseRequiredAttribute<int>("attribute");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void CoverageParseUtils_ParseRequiredAttribute_AttributeNotFound_ThrowsCoverageParseException() {
        XAttribute attribute = new("attribute", "42");
        XElement element = new("element", attribute);

        Assert.Throws<CoverageParseException>(() => element.ParseRequiredAttribute<int>("unknown-attribute"));
    }

    [Test]
    public void CoverageParseUtils_ParseRequiredAttribute_AttributeFound_InvalidValue_ThrowsCoverageParseException() {
        XAttribute attribute = new("attribute", "invalid");
        XElement element = new("element", attribute);

        Assert.Throws<CoverageParseException>(() => element.ParseRequiredAttribute<int>("attribute"));
    }
}