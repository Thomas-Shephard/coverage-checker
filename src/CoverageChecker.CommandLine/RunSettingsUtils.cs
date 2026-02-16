using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace CoverageChecker.CommandLine;

internal static class RunSettingsUtils
{
    public static void ApplyRunSettings(CommandLineOptions options, ILogger logger, out IEnumerable<string>? include, out IEnumerable<string>? exclude)
    {
        include = options.Include;
        exclude = options.Exclude;

        if (string.IsNullOrEmpty(options.RunSettings))
        {
            return;
        }

        if (!File.Exists(options.RunSettings))
        {
            if (options.RunSettings != ".runsettings")
            {
                FileNotFoundException ex = new("The specified .runsettings file was not found.");
                logger.LogRunSettingsParseFailed(ex, options.RunSettings);
                throw ex;
            }

            return;
        }

        try
        {
            XDocument doc = XDocument.Load(options.RunSettings);
            XElement? config = doc.Descendants()
                                  .FirstOrDefault(e => e.Name.LocalName == "DataCollector" &&
                                                       string.Equals((string?)e.Attribute("friendlyName"), "XPlat Code Coverage", StringComparison.OrdinalIgnoreCase))?
                                  .Elements()
                                  .FirstOrDefault(e => e.Name.LocalName == "Configuration");

            if (config == null)
            {
                return;
            }

            // Note: We deliberately ignore 'Include' and 'Exclude' as they are Assembly/Type filters in XPlat Code Coverage,
            // which are incompatible with the file path globbing used here. Only 'ExcludeByFile' is supported.
            if (exclude != null && exclude.Any())
                return;

            string? excludeByFileStr = (string?)config.Elements().FirstOrDefault(e => e.Name.LocalName == "ExcludeByFile");
            if (!string.IsNullOrWhiteSpace(excludeByFileStr))
            {
                exclude = excludeByFileStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }
        catch (Exception ex)
        {
            logger.LogRunSettingsParseFailed(ex, options.RunSettings);
        }
    }
}
