namespace CoverageChecker.Services;

/// <summary>
/// Service for interacting with Git to identify changed lines.
/// </summary>
internal interface IGitService
{
    /// <summary>
    /// Gets the lines that have been changed (added or modified) between the specified base and head.
    /// </summary>
    /// <param name="base">The base branch or commit to compare against.</param>
    /// <param name="head">The head branch or commit. Defaults to "HEAD".</param>
    /// <returns>A dictionary where the key is the file path and the value is a set of changed line numbers.</returns>
    IDictionary<string, HashSet<int>> GetChangedLines(string @base, string head = "HEAD");

    /// <summary>
    /// Gets the files that have been renamed between the specified base and head.
    /// </summary>
    /// <param name="base">The base branch or commit to compare against.</param>
    /// <param name="head">The head branch or commit. Defaults to "HEAD".</param>
    /// <param name="renameThreshold">The similarity threshold for rename detection (0.0 to 1.0). Defaults to 0.5 (50%).</param>
    /// <returns>A dictionary where the key is the old file path and the value is the new file path.</returns>
    IDictionary<string, string> GetRenames(string @base, string head = "HEAD", double renameThreshold = 0.5);

    /// <summary>
    /// Gets the root directory of the git repository.
    /// </summary>
    /// <returns>The root directory of the git repository.</returns>
    string GetRepoRoot();
}