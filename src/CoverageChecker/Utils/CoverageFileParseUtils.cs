using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Linq;

namespace CoverageChecker.Utils;

internal static class CoverageFileParseUtils {
    internal static void ReadElement(this XmlReader reader, string elementName) {
        // Read the element and check if it is the expected element if not continue reading at this level until we find the expected element
        if (reader.NodeType == XmlNodeType.Element && reader.Name == elementName)
            return;
        else if (!reader.ReadToFollowing(elementName))
            throw new CoverageParseException($"Element '{elementName}' not found");
    }

    internal static async Task ReadElementAndParse(this XmlReader reader, string elementName, Func<Task> parseAction) {
        // Read Element and end read the element
        // Can we use try and finally block here?

        try {
            reader.ReadElement(elementName);
            await parseAction();
        } finally {
            reader.ReadToEndOfElementAtCurrentDepth();
        }
    }

    internal static async Task ReadElementsAndParse(this XmlReader reader, string elementName, Func<Task> parseAction) {

        // This is called when we have multiple elements of the same type at the same level
        // e.g.
        // <root>
        //     <element></element>
        //     <element></element>
        //     <element></element>
        // </root>

        // We want to parse the element and then move to the next element at the same level and parse it until we reach the end of the parent element
        // It must be within the same level

        // If root is a self closing element, there are no elements to parse and we should not move to the next element
        if (reader.IsEmptyElement)
            return;

        int depth = reader.Depth;

        bool isFirst = true;

        while ((isFirst || reader.Depth > depth) && await reader.ReadAsync() && reader.Depth > depth) {
            isFirst = false;
            if (reader.NodeType == XmlNodeType.Element && reader.Name == elementName) {
                await parseAction();
            }
        }
    }

    internal static async Task ReadNestedElementsAndParse(this XmlReader reader, string parentElementName, string elementName, Func<Task> parseAction) {
        // Read the parent element and then read the nested elements and parse them
        // e.g.
        // <root>
        //     <parent>
        //         <element></element>
        //         <element></element>
        //         <element></element>
        //     </parent>
        // </root>

        // We want to parse the parent element and then move to the nested elements and parse them until we reach the end of the parent element
        // It must be within the same level

        reader.ReadElement(parentElementName);

        await reader.ReadElementsAndParse(elementName, parseAction);
    }

    internal static void ReadToEndOfElementAtCurrentDepth(this XmlReader reader) {
        // Read to the end of the current element
        // This is useful when we want to skip the current element and move to the next element at the same level

        int depth = reader.Depth;

        while (reader.Read() && reader.Depth > depth) { }
    }


    internal static string GetRequiredAttribute(this XmlReader reader, string attributeName) {
        string? attributeValue = reader.GetAttribute(attributeName);

        if (attributeValue is null)
            throw new CoverageParseException($"Attribute '{attributeName}' not found");

        return attributeValue;
    }
}