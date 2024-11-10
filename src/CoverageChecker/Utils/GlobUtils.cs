using Microsoft.Extensions.FileSystemGlobbing;

namespace CoverageChecker.Utils;

internal static class GlobUtils
{
    internal static Matcher AddGlobPatterns(this Matcher matcher, IEnumerable<string> globPatterns)
    {
        globPatterns = globPatterns.ToArray();

        if (!globPatterns.Any())
        {
            throw new ArgumentException("At least one glob pattern must be provided");
        }

        foreach (string pattern in globPatterns)
        {
            // If the pattern starts with an ! it is treated as an exclude pattern otherwise it is an include pattern
            if (pattern.StartsWith('!'))
            {
                matcher.AddExclude(pattern[1..]);
            }
            else
            {
                matcher.AddInclude(pattern);
            }
        }

        return matcher;
    }
}