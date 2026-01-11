using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CoverageChecker.Utils;

namespace CoverageChecker.Services;

internal partial class GitService : IGitService
{
    private readonly IProcessExecutor _executor;
    private static readonly Regex DiffHeaderRegex = DiffHeaderGeneratedRegex();
    private static readonly Regex FileHeaderRegex = FileHeaderGeneratedRegex();

    internal GitService(IProcessExecutor executor)
    {
        _executor = executor;
    }

    public GitService() : this(new ProcessExecutor())
    {
    }

    public IDictionary<string, HashSet<int>> GetChangedLines(string @base, string head = "HEAD")
    {
        if (@base.StartsWith('-'))
        {
            throw new ArgumentException("Base reference cannot start with '-'.", nameof(@base));
        }

        if (head.StartsWith('-'))
        {
            throw new ArgumentException("Head reference cannot start with '-'.", nameof(head));
        }

        string repoRoot = GetRepoRoot();
        Dictionary<string, HashSet<int>> changedLines = [];

        // -c core.quotepath=false: Ensure non-ASCII chars are output as UTF-8 bytes, not octal escapes.
        // --src-prefix=a/ --dst-prefix=b/: Force prefixes regardless of user config.
        string[] arguments =
        [
            "-c", "core.quotepath=false", "diff", "-U0", "--no-color", "--no-ext-diff",
            "--src-prefix=a/", "--dst-prefix=b/", @base, head, "--"
        ];

        (int exitCode, string stdout, string stderr) result;
        try
        {
            result = _executor.Execute("git", arguments);
        }
        catch (Win32Exception ex)
        {
            throw new GitException("Failed to execute 'git'. Ensure Git is installed and in your PATH.", ex);
        }

        if (result.exitCode != 0)
        {
            throw new GitException($"Git diff failed with exit code {result.exitCode}: {result.stderr}");
        }

        string? currentFile = null;

        using StringReader reader = new(result.stdout);
        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith("diff --git "))
            {
                currentFile = null;
            }

            Match fileMatch = FileHeaderRegex.Match(line);
            if (fileMatch.Success)
            {
                string relativePath = fileMatch.Groups[1].Success
                    ? fileMatch.Groups[1].Value
                    : UnescapeGitPath(fileMatch.Groups[2].Value);

                currentFile = PathUtils.GetNormalizedFullPath(Path.Combine(repoRoot, relativePath));

                if (!changedLines.ContainsKey(currentFile))
                {
                    changedLines[currentFile] = [];
                }
                continue;
            }

            if (currentFile == null) continue;

            Match diffMatch = DiffHeaderRegex.Match(line);
            if (!diffMatch.Success) continue;
            int startLine = int.Parse(diffMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            int lineCount = diffMatch.Groups[2].Success ? int.Parse(diffMatch.Groups[2].Value, CultureInfo.InvariantCulture) : 1;

            for (int i = 0; i < lineCount; i++)
            {
                changedLines[currentFile].Add(startLine + i);
            }
        }

        return changedLines;
    }

    public string GetRepoRoot()
    {
        (int exitCode, string stdout, string stderr) result;
        try
        {
            result = _executor.Execute("git", ["rev-parse", "--show-toplevel"]);
        }
        catch (Win32Exception ex)
        {
            throw new GitException("Failed to execute 'git'. Ensure Git is installed and in your PATH.", ex);
        }

        if (result.exitCode != 0)
        {
            throw new GitException($"Failed to get git repo root: {result.stderr}");
        }

        return result.stdout.Trim();
    }

    private static string UnescapeGitPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;

        StringBuilder sb = new();
        int i = 0;
        while (i < path.Length)
        {
            char c = path[i];
            if (c == '\\' && i + 1 < path.Length)
            {
                char next = path[i + 1];
                if (IsOctal(next) && i + 3 < path.Length && IsOctal(path[i + 2]) && IsOctal(path[i + 3]))
                {
                    List<byte> bytes = [];
                    while (i + 3 < path.Length && path[i] == '\\' && IsOctal(path[i + 1]) && IsOctal(path[i + 2]) && IsOctal(path[i + 3]))
                    {
                        bytes.Add((byte)Convert.ToInt32(path.Substring(i + 1, 3), 8));
                        i += 4;
                    }
                    sb.Append(Encoding.UTF8.GetString(bytes.ToArray()));
                    continue;
                }

                sb.Append(next switch
                {
                    '"' => '"',
                    '\\' => '\\',
                    'n' => '\n',
                    't' => '\t',
                    'b' => '\b',
                    'f' => '\f',
                    'r' => '\r',
                    'v' => '\v',
                    'a' => '\a',
                    'e' => '\u001b',
                    _ => next
                });
                i += 2;
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }
        return sb.ToString();
    }

    private static bool IsOctal(char c) => c is >= '0' and <= '7';

    [GeneratedRegex("""^@@\s*-\d+(?:,\d+)?\s+\+(\d+)(?:,(\d+))?\s*@@""", RegexOptions.Compiled)]
    private static partial Regex DiffHeaderGeneratedRegex();
    [GeneratedRegex("""^\+\+\+\s+(?:b/((?:(?![\t\n]).)*?)|"b/((?:[^"\\]|\\.)+)")(?:\s+\d{4}-\d{2}-\d{2}.*)?$""", RegexOptions.Compiled)]
    private static partial Regex FileHeaderGeneratedRegex();
}