using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace CoverageChecker.Utils;

internal static class CoverageFileParseUtils {
    internal static bool TryEnterElement(this XmlReader reader, string elementName, Action action, bool throwIfNotFound = true) {
        if (reader.NodeType == XmlNodeType.None)
            reader.Read();

        int depth = reader.Depth;

        if (reader.IsStartOfElement(elementName)) {
            bool isEmptyElement = reader.IsEmptyElement;
            if (!isEmptyElement) {
                reader.Read();
                action();
            }

            reader.ConsumeElement(elementName, depth);
            return !isEmptyElement;
        }

        if (throwIfNotFound)
            throw new CoverageParseException($"Element '{elementName}' not found");
        return false;
    }

    private static bool IsStartOfElement(this XmlReader reader, string elementName) {
        return reader.NodeType == XmlNodeType.Element && reader.Name == elementName;
    }

    internal static void ParseElements(this XmlReader reader, string elementName, Action action) {
        int depth = reader.Depth;

        while (depth == reader.Depth) {
            if (reader.IsStartOfElement(elementName)) {
                action();

                if (reader.Depth == depth && reader.Name == elementName && reader.NodeType == XmlNodeType.Element)
                    continue;
            }

            if (reader.ConsumeElement(elementName, depth))
                return;
        }
    }

    internal static bool ConsumeElement(this XmlReader reader, string elementName, int? depth = null) {
        depth ??= reader.Depth;

        if (reader.Depth < depth)
            return true;

        if (reader.Depth == depth && reader.NodeType == XmlNodeType.EndElement) {
            // No need to read to the end element if we are already at the end element
        } else if (!reader.IsEmptyElement || reader.Depth > depth) {
            while (reader.Read() && reader.Depth > depth) { }

            if (reader.NodeType != XmlNodeType.EndElement || reader.Name != elementName)
                throw new CoverageParseException($"Expected end element '{elementName}' but found '{reader.Name}'");
        }

        reader.Read();
        return false;
    }

    internal static bool TryGetAttribute<T>(this XmlReader reader, string attributeName, [NotNullWhen(true)] out T? value) where T : IParsable<T> {
        string? attributeValue = reader.GetAttribute(attributeName);

        if (attributeValue is null) {
            value = default;
            return false;
        }

        if (!T.TryParse(attributeValue, null, out T? parsedValue))
            throw new CoverageParseException($"Failed to parse attribute '{attributeName}' on node '{reader.Name}'");

        value = parsedValue;
        return true;
    }

    internal static T GetRequiredAttribute<T>(this XmlReader reader, string attributeName) where T : IParsable<T> {
        if (!reader.TryGetAttribute(attributeName, out T? value))
            throw new CoverageParseException($"Attribute '{attributeName}' not found on element '{reader.Name}'");

        return value;
    }
}