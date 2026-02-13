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

    public GitService() : this(new ProcessExecutor()) { }

    public IDictionary<string, HashSet<int>> GetChangedLines(string @base, string head = "HEAD")
    {
        ValidateGitReferences(@base, head);
        string repoRoot = GetRepoRoot();
        string diffOutput = ExecuteGitDiff(@base, head);
        return ParseGitDiff(diffOutput, repoRoot);
    }

    private static void ValidateGitReferences(string @base, string head)
    {
        if (@base.StartsWith('-'))
        {
            throw new ArgumentException("Base reference cannot start with '-'.", nameof(@base));
        }

        if (head.StartsWith('-'))
        {
            throw new ArgumentException("Head reference cannot start with '-'.", nameof(head));
        }
    }

    private string ExecuteGitDiff(string @base, string head)
    {
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

        return result.stdout;
    }

    private static Dictionary<string, HashSet<int>> ParseGitDiff(string diffOutput, string repoRoot)
    {
        Dictionary<string, HashSet<int>> changedLines = [];
        string? currentFile = null;

        using StringReader reader = new(diffOutput);
        while (reader.ReadLine() is { } line)
        {
            if (TryUpdateCurrentFile(line, repoRoot, changedLines, out string? newFile))
            {
                currentFile = newFile;
                continue;
            }

            if (currentFile != null)
            {
                ProcessDiffLine(line, currentFile, changedLines);
            }
        }

        return changedLines;
    }

    private static bool TryUpdateCurrentFile(string line, string repoRoot, Dictionary<string, HashSet<int>> changedLines, out string? newFile)
    {
        if (line.StartsWith("diff --git ", StringComparison.Ordinal))
        {
            newFile = null;
            return true;
        }

        Match fileMatch = FileHeaderRegex.Match(line);
        if (fileMatch.Success)
        {
            string relativePath = fileMatch.Groups[1].Success
                ? fileMatch.Groups[1].Value
                : UnescapeGitPath(fileMatch.Groups[2].Value);

            newFile = PathUtils.GetNormalizedFullPath(Path.Combine(repoRoot, relativePath));

            if (!changedLines.ContainsKey(newFile))
            {
                changedLines[newFile] = [];
            }
            return true;
        }

        newFile = null;
        return false;
    }

    private static void ProcessDiffLine(string line, string currentFile, Dictionary<string, HashSet<int>> changedLines)
    {
        Match diffMatch = DiffHeaderRegex.Match(line);
        if (!diffMatch.Success)
            return;
        int startLine = int.Parse(diffMatch.Groups[1].Value, CultureInfo.InvariantCulture);
        int lineCount = diffMatch.Groups[2].Success ? int.Parse(diffMatch.Groups[2].Value, CultureInfo.InvariantCulture) : 1;

        for (int i = 0; i < lineCount; i++)
        {
            changedLines[currentFile].Add(startLine + i);
        }
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

        return PathUtils.GetNormalizedFullPath(result.stdout.Trim());
    }

    private static string UnescapeGitPath(string path)
    {
        StringBuilder sb = new();
        int i = 0;
        while (i < path.Length)
        {
            char c = path[i];
            if (c == '\\' && i + 1 < path.Length)
            {
                i = HandleEscapedCharacter(path, i, sb);
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }
        return sb.ToString();
    }

    private static int HandleEscapedCharacter(string path, int index, StringBuilder sb)
    {
        char next = path[index + 1];
        if (IsOctal(next) && index + 3 < path.Length && IsOctal(path[index + 2]) && IsOctal(path[index + 3]))
        {
            return DecodeOctalSequence(path, index, sb);
        }

        sb.Append(DecodeEscapeChar(next));
        return index + 2;
    }

    private static int DecodeOctalSequence(string path, int index, StringBuilder sb)
    {
        List<byte> bytes = [];
        int i = index;
        while (i + 3 < path.Length && path[i] == '\\' && IsOctal(path[i + 1]) && IsOctal(path[i + 2]) && IsOctal(path[i + 3]))
        {
            int val = ((path[i + 1] - '0') << 6) | ((path[i + 2] - '0') << 3) | (path[i + 3] - '0');
            bytes.Add((byte)val);
            i += 4;
        }
        sb.Append(Encoding.UTF8.GetString(bytes.ToArray()));
        return i;
    }

    private static char DecodeEscapeChar(char c) => c switch
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
        _ => c
    };

    private static bool IsOctal(char c) => c is >= '0' and <= '7';

    [GeneratedRegex("""^@@\s*-\d+(?:,\d+)?\s+\+(\d+)(?:,(\d+))?\s*@@""", RegexOptions.Compiled)]
    private static partial Regex DiffHeaderGeneratedRegex();
    [GeneratedRegex("""^\+\+\+\s+(?:b/([^\s]*)|"b/((?>[^"\\]|\\.)+)")(?:\s+\d{4}-\d{2}-\d{2}.*)?$""", RegexOptions.Compiled)]
    private static partial Regex FileHeaderGeneratedRegex();
}