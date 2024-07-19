using System.Reflection;
using System.Text;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;

namespace CoverageChecker.CommandLineInterface;

internal static class ParseUtils {
    internal static ParserResult<T> ThrowOnParseError<T>(this ParserResult<T> parserResult, ILogger logger) {
        if (parserResult is not NotParsed<T>)
            return parserResult;

        SentenceBuilder builder = SentenceBuilder.Create();
        string[] errorMessages = HelpText.RenderParsingErrorsTextAsLines(parserResult, builder.FormatError, builder.FormatMutuallyExclusiveSetErrors, 1)
                                         .ToArray();

        if (errorMessages.Length is 0)
            return parserResult;

        logger.LogError("Error with arguments:{NewLine}{Errors}", Environment.NewLine, string.Join(Environment.NewLine, errorMessages));

        Exception[] exceptions = errorMessages.Select(message => (Exception)new ArgumentException(message))
                                              .ToArray();

        throw new AggregateException(exceptions);
    }

    internal static ParserResult<T> LogParsedArguments<T>(this ParserResult<T> parserResult, ILogger logger) {
        T? parsedArguments = parserResult.Value;

        if (parsedArguments is null)
            return parserResult;

        StringBuilder parsedArgumentsLog = new();

        PropertyInfo[] properties = parsedArguments.GetType().GetProperties();
        foreach (PropertyInfo property in properties) {
            object? value = property.GetValue(parsedArguments);

            // If the value is an enumerable, convert it to a comma-separated string
            // A special case is made for strings as they are IEnumerable<char>
            if (value is not string && value is IEnumerable<object> enumerable) {
                // Convert the enumerable to an array to avoid multiple enumeration
                enumerable = enumerable as object[] ?? enumerable.ToArray();
                value = enumerable.Any() ? string.Join(", ", enumerable) : "<empty>";
            }

            parsedArgumentsLog.AppendLine($" {property.Name}: {value}");
        }

        logger.LogInformation("Parsed arguments:{NewLine}{ParsedArguments}", Environment.NewLine, parsedArgumentsLog);

        return parserResult;
    }
}