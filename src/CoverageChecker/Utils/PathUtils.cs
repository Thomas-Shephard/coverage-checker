using System.Runtime.InteropServices;

namespace CoverageChecker.Utils;

internal enum PathStyle
{
    Windows,
    Unix
}

internal static class PathUtils
{
    private static PathStyle? _styleOverride;

    internal static IDisposable OverrideStyle(PathStyle style)
    {
        PathStyle? previous = _styleOverride;
        _styleOverride = style;
        return new StyleOverrideScope(previous);
    }

    private sealed class StyleOverrideScope(PathStyle? previous) : IDisposable
    {
        public void Dispose() => _styleOverride = previous;
    }

    private static PathStyle CurrentStyle => _styleOverride ?? (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PathStyle.Windows : PathStyle.Unix);

    /// <summary>
    /// Gets a string comparer that is appropriate for the current operating system's file system.
    /// </summary>
    public static StringComparer PathComparer => CurrentStyle == PathStyle.Windows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>
    /// Checks if a path is rooted (either Windows-style C:\ or Unix-style /).
    /// </summary>
    public static bool IsPathRooted(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        // Unix-style or UNC/Absolute path starting with slash
        if (path[0] == '/' || path[0] == '\\')
            return true;

        // Windows-style drive letter (e.g., C:)
        return path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':';
    }

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

    /// <summary>
    /// Gets the normalized absolute path.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <param name="basePath">Optional: The base path to resolve the path against if it is relative.</param>
    /// <returns>The normalized absolute path.</returns>
    public static string GetNormalizedFullPath(string path, string? basePath = null)
    {
        if (basePath != null && !IsPathRooted(path))
        {
            path = NormalizePath(basePath).TrimEnd('/') + "/" + NormalizePath(path).TrimStart('/');
        }

        try
        {
            // Only call Path.GetFullPath if the path style matches the current OS
            // to avoid prepending current drive letters to Unix paths on Windows.
            bool isCurrentOsStyle = (CurrentStyle == PathStyle.Windows && RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ||
                                    (CurrentStyle == PathStyle.Unix && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            if (isCurrentOsStyle)
            {
                return NormalizePath(Path.GetFullPath(path));
            }
            
            return NormalizePath(path);
        }
        catch
        {
            return NormalizePath(path);
        }
    }

    /// <summary>
    /// Makes a path relative to a directory if it is rooted and inside that directory.
    /// </summary>
    /// <param name="directory">The directory to make the path relative to.</param>
    /// <param name="path">The path to process.</param>
    /// <param name="basePath">Optional: The base path to resolve the path against if it is relative.</param>
    /// <returns>The relative path if it was inside the directory, otherwise the original path.</returns>
    public static string MakeRelativeIfInside(string directory, string path, string? basePath = null)
    {
        if (string.IsNullOrEmpty(path) || !IsPathRooted(path))
        {
            return path;
        }

        string fullPath = GetNormalizedFullPath(path, basePath);
        string fullDirectory = GetNormalizedFullPath(directory, basePath);

        if (IsSubPathOf(fullDirectory, fullPath))
        {
            if (fullPath.Length == fullDirectory.Length)
                return ".";

            return fullPath[fullDirectory.Length..].TrimStart('/');
        }

        return fullPath;
    }

    /// <summary>
    /// Checks if a path is a sub-path of a parent directory.
    /// </summary>
    /// <param name="parentDirectory">The parent directory.</param>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is a sub-path of the parent directory.</returns>
    public static bool IsSubPathOf(string parentDirectory, string path)
    {
        string fullPath = NormalizePath(path);
        string fullParent = NormalizePath(parentDirectory);

        StringComparison comparison = PathComparer == StringComparer.OrdinalIgnoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (fullPath.StartsWith(fullParent, comparison))
        {
            // Ensure we don't match "C:/repo_suffix" when directory is "C:/repo"
            return fullPath.Length == fullParent.Length || fullPath[fullParent.Length] == '/';
        }

        return false;
    }
}