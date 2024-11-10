using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace CoverageChecker.Utils;

internal static class CoverageFileParseUtils
{
    internal static void ConsumeElement(this XmlReader reader, string elementName, int? depth = null)
    {
        depth ??= reader.Depth;

        if (reader.Depth < depth)
            throw new CoverageParseException($"Expected to consume element '{elementName}' but it had already been consumed");

        bool startedAtEntryDepth = reader.Depth == depth;

        bool startedWithEmptyElement = startedAtEntryDepth && reader.IsEmptyElement;
        bool startedWithEndElement = startedAtEntryDepth && reader.NodeType == XmlNodeType.EndElement;

        // If the reader is not at either of the following at the depth of the element to be consumed:
        //   1. An empty element
        //   2. A closing element
        // Then the reader should continue reading until the end of the element to be consumed
        if (!startedWithEmptyElement && !startedWithEndElement)
        {
            while (reader.Read() && reader.Depth > depth)
            {
                // Continue reading until the element has been consumed
            }
        }

        // The reader should now be at an EndElement or empty element for the provided input element details

        // If the element name is incorrect, an incorrect element has been consumed so an exception should be thrown
        if (reader.Name != elementName)
            throw new CoverageParseException($"Expected to consume element '{elementName}' but found '{reader.Name}'");

        // If the reader did not start at an empty element of the desired depth, and we are not now at the end of the element,
        // this method must have been called before the element was entered so an exception should be thrown
        if (!startedWithEmptyElement && reader.NodeType != XmlNodeType.EndElement)
            throw new CoverageParseException($"Expected to consume element '{elementName}' but it had not been entered");

        // Read past the end of the element to fully consume it
        reader.Read();
    }

    internal static bool TryEnterElement(this XmlReader reader, string elementName, Action action, bool throwIfNotFound = true)
    {
        int depth = reader.Depth;

        // Check that an element with the provided name is the next element to be read
        if (reader.NodeType == XmlNodeType.Element && reader.Name == elementName)
        {
            bool enteredElement = false;

            // If the element is not empty, read past the start of the element
            if (!reader.IsEmptyElement)
            {
                reader.Read();

                // If the element has contents (i.e. not immediately the closing element) execute the action
                if (reader.Depth != depth || reader.NodeType != XmlNodeType.EndElement)
                {
                    enteredElement = true;
                    action();
                }
            }

            // Consume the element and return if the action was executed
            reader.ConsumeElement(elementName, depth);
            return enteredElement;
        }

        if (throwIfNotFound)
            throw new CoverageParseException($"Expected to find start element '{elementName}' but found '{reader.Name}' of type '{reader.NodeType}'");

        return false;
    }

    internal static void ParseElements(this XmlReader reader, string elementName, Action action)
    {
        int depth = reader.Depth;

        if (reader.NodeType != XmlNodeType.Element)
            throw new CoverageParseException($"Expected to find start element but found '{reader.Name}' of type '{reader.NodeType}'");

        while (depth == reader.Depth)
        {
            // If the element name does not match or this is not the start of an element consume the current element
            if (!(reader.NodeType == XmlNodeType.Element && reader.Name == elementName))
            {
                reader.ConsumeElement(reader.Name);
                continue;
            }

            // Parse the element if it matches
            action();
        }
    }

    internal static bool TryGetAttribute<T>(this XmlReader reader, string attributeName, [NotNullWhen(true)] out T? value) where T : IParsable<T>
    {
        string? attributeValue = reader.GetAttribute(attributeName);

        if (attributeValue is null)
        {
            value = default;
            return false;
        }

        if (!T.TryParse(attributeValue, null, out T? parsedValue))
            throw new CoverageParseException($"Failed to parse attribute '{attributeName}' on node '{reader.Name}'");

        value = parsedValue;
        return true;
    }

    internal static T GetRequiredAttribute<T>(this XmlReader reader, string attributeName) where T : IParsable<T>
    {
        if (!reader.TryGetAttribute(attributeName, out T? value))
            throw new CoverageParseException($"Attribute '{attributeName}' not found on element '{reader.Name}'");

        return value;
    }
}