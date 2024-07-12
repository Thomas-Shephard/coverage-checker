using System.Xml.Linq;

namespace CoverageChecker.Utils;

internal static class CoverageFileParseUtils {
    internal static XElement GetRequiredElement(this XContainer container, XName name) {
        return container.Element(name) ?? throw new CoverageParseException($"Element '{name}' not found");
    }

    internal static XAttribute GetRequiredAttribute(this XElement element, XName name) {
        return element.Attribute(name) ?? throw new CoverageParseException($"Attribute '{name}' not found on element '{element.Name}'");
    }

    internal static T? ParseOptionalAttribute<T>(this XElement element, XName name, IFormatProvider? provider = null) where T : struct, IParsable<T> {
        string? attributeValue = element.Attribute(name)?.Value;

        if (attributeValue is null)
            return null;

        // If the attribute is found, try to parse the value into the specified type
        if (!T.TryParse(attributeValue, provider, out T value))
            throw new CoverageParseException($"Attribute '{name}' on element '{element.Name}' is not a valid {typeof(T).Name}");

        return value;
    }

    internal static T ParseRequiredAttribute<T>(this XElement element, XName name, IFormatProvider? provider = null) where T : IParsable<T> {
        string attributeValue = element.GetRequiredAttribute(name).Value;

        // Try to parse the attribute value into the specified type
        if (!T.TryParse(attributeValue, provider, out T? value))
            throw new CoverageParseException($"Attribute '{name}' on element '{element.Name}' is not a valid {typeof(T).Name}");

        return value;
    }
}