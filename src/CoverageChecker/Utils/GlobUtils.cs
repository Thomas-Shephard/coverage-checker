using Microsoft.Extensions.FileSystemGlobbing;

namespace CoverageChecker.Utils;

internal static class GlobUtils {
    internal static IEnumerable<string> GetFilePathsFromGlobPatterns(string directory, IEnumerable<string> globPatterns) {
        return new Matcher().AddGlobPatterns(globPatterns)
                            .GetResultsInFullPath(directory);
    }

    internal static Matcher AddGlobPatterns(this Matcher matcher, IEnumerable<string> globPatterns) {
        foreach (string pattern in globPatterns) {
            // If the pattern starts with an ! it is treated as an exclude pattern otherwise it is an include pattern
            if (pattern.StartsWith('!')) {
                matcher.AddExclude(pattern[1..]);
            } else {
                matcher.AddInclude(pattern);
            }
        }

        return matcher;
    }
}