using Microsoft.Extensions.FileSystemGlobbing;

namespace CoverageChecker.Utils;

internal static class GlobUtils {
    internal static Matcher CreateFromGlobPatterns(IEnumerable<string> globPatterns, Matcher? matcher = null) {
        matcher ??= new Matcher();

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