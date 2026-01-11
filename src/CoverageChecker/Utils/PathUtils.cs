namespace CoverageChecker.Utils;

internal static class PathUtils
{
    /// <summary>
    /// Normalizes a path to use the universal '/' separator and removes trailing slashes.
    /// This ensures consistency across different operating systems and when merging reports.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    public static string NormalizePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        string normalized = path.Replace('\\', '/');
        string trimmed = normalized.TrimEnd('/');

        return trimmed switch
        {
            "" when normalized.Length > 0 => "/",
            _ when trimmed.Length == 2 && char.IsLetter(trimmed[0]) && trimmed[1] == ':' => trimmed + "/",
            _ => trimmed
        };
    }
}