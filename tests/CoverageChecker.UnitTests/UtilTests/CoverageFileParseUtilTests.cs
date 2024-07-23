using System.Xml.Linq;
using CoverageChecker.Utils;

namespace CoverageChecker.UnitTests.UtilTests;

public class CoverageFileParseUtilTests {
    private const string ElementName = "element";
    private const string ContainerName = "container";
    private const string AttributeName = "attribute";
    private const int AttributeNumericValue = 42;
    private const string AttributeStringValue = "invalid";

    private static readonly XElement Element = new(ElementName);
    private static readonly XElement ContainerWithElement = new(ContainerName, Element);
    private static readonly XAttribute AttributeWithNumericValue = new(AttributeName, AttributeNumericValue);
    private static readonly XElement ElementWithAttributeWithNumericValue = new(ElementName, AttributeWithNumericValue);
    private static readonly XAttribute AttributeWithStringValue = new(AttributeName, AttributeStringValue);
    private static readonly XElement ElementWithAttributeWithStringValue = new(ElementName, AttributeWithStringValue);

    [Test]
    public void CoverageParseUtils_GetRequiredElement_ElementFound_ReturnsElement() {
        XElement result = ContainerWithElement.GetRequiredElement(ElementName);

        Assert.That(result, Is.EqualTo(Element));
    }

    [Test]
    public void CoverageParseUtils_GetRequiredElement_ElementNotFound_ThrowsCoverageParseException() {
        Exception e = Assert.Throws<CoverageParseException>(() => ContainerWithElement.GetRequiredElement($"unknown-{ElementName}"));
        Assert.That(e.Message, Is.EqualTo("Element 'unknown-element' not found"));
    }

    [Test]
    public void CoverageParseUtils_GetRequiredAttribute_AttributeFound_ReturnsAttribute() {
        XAttribute result = ElementWithAttributeWithStringValue.GetRequiredAttribute(AttributeName);

        Assert.Multiple(() => {
            Assert.That(result, Is.EqualTo(AttributeWithStringValue));
            Assert.That(result.Value, Is.EqualTo(AttributeStringValue));
        });
    }

    [Test]
    public void CoverageParseUtils_GetRequiredAttribute_AttributeNotFound_ThrowsCoverageParseException() {
        Exception e = Assert.Throws<CoverageParseException>(() => ElementWithAttributeWithStringValue.GetRequiredAttribute($"unknown-{AttributeName}"));
        Assert.That(e.Message, Is.EqualTo("Attribute 'unknown-attribute' not found on element 'element'"));
    }

    [Test]
    public void CoverageParseUtils_ParseOptionalAttribute_AttributeFoundWithValidValue_ReturnsValue() {
        int? attributeValue = ElementWithAttributeWithNumericValue.ParseOptionalAttribute<int>(AttributeName);

        Assert.That(attributeValue, Is.EqualTo(AttributeNumericValue));
    }

    [Test]
    public void CoverageParseUtils_ParseOptionalAttribute_AttributeFoundWithInvalidValue_ThrowsCoverageParseException() {
        Exception e = Assert.Throws<CoverageParseException>(() => ElementWithAttributeWithStringValue.ParseOptionalAttribute<int>(AttributeName));
        Assert.That(e.Message, Is.EqualTo("Attribute 'attribute' on element 'element' is not a valid Int32"));
    }

    [Test]
    public void CoverageParseUtils_ParseOptionalAttribute_AttributeNotFound_ReturnsNull() {
        int? attributeValue = ElementWithAttributeWithNumericValue.ParseOptionalAttribute<int>($"unknown-{AttributeName}");

        Assert.That(attributeValue, Is.Null);
    }

    [Test]
    public void CoverageParseUtils_ParseRequiredAttribute_AttributeFoundWithValidValue_ReturnsValue() {
        int attributeValue = ElementWithAttributeWithNumericValue.ParseRequiredAttribute<int>(AttributeName);

        Assert.That(attributeValue, Is.EqualTo(AttributeNumericValue));
    }

    [Test]
    public void CoverageParseUtils_ParseRequiredAttribute_AttributeFoundWithInvalidValue_ThrowsCoverageParseException() {
        Exception e = Assert.Throws<CoverageParseException>(() => ElementWithAttributeWithStringValue.ParseRequiredAttribute<int>(AttributeName));
        Assert.That(e.Message, Is.EqualTo("Attribute 'attribute' on element 'element' is not a valid Int32"));
    }

    [Test]
    public void CoverageParseUtils_ParseRequiredAttribute_AttributeNotFound_ThrowsCoverageParseException() {
        Exception e = Assert.Throws<CoverageParseException>(() => ElementWithAttributeWithNumericValue.ParseRequiredAttribute<int>($"unknown-{AttributeName}"));
        Assert.That(e.Message, Is.EqualTo("Attribute 'unknown-attribute' not found on element 'element'"));
    }
}