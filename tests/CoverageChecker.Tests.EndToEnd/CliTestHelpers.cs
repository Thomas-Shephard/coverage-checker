using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CoverageChecker.Tests.EndToEnd;

internal abstract class CommandLineTestBase
{
    protected static readonly string CoberturaCoverageFiles = Path.Combine(TestContext.CurrentContext.TestDirectory, "CoverageFiles", "Cobertura");

    protected static string GetCliPath()
    {
        string? envPath = Environment.GetEnvironmentVariable("COVERAGE_CHECKER_CLI_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return envPath;

        string baseDir = AppContext.BaseDirectory;
        DirectoryInfo? dir = new(baseDir);

        // Find the project root by looking for the solution or artifacts folder
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "artifacts")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
            throw new InvalidOperationException("Could not find artifacts folder");

        string artifactsDir = Path.Combine(dir.FullName, "artifacts", "bin", "CoverageChecker.CommandLine");
        string exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "CoverageChecker.CommandLine.exe" : "CoverageChecker.CommandLine";

        // Try to find the executable in the same artifact target folder as the current test run.
        // baseDir example: .../artifacts/bin/CoverageChecker.Tests.EndToEnd/debug_net8.0
        string targetFolderName = new DirectoryInfo(baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).Name;
        string? exePath = Directory.GetFiles(artifactsDir, exeName, SearchOption.AllDirectories)
                                   .FirstOrDefault(f => f.Contains(targetFolderName) && !f.Contains("publish"));

        // Fallback to any match if configuration-specific one isn't found
        exePath ??= Directory.GetFiles(artifactsDir, exeName, SearchOption.AllDirectories)
                                .FirstOrDefault(f => !f.Contains("publish"));

        return exePath ?? throw new InvalidOperationException($"Could not find {exeName} executable in {artifactsDir}. Set COVERAGE_CHECKER_CLI_PATH to override.");
    }

    protected static async Task<(int ExitCode, string Stdout)> RunCli(params string[] arguments)
    {
        return await RunCliInDirectory(Environment.CurrentDirectory, arguments);
    }

    protected static async Task<(int ExitCode, string Stdout)> RunCliInDirectory(string workingDirectory, params string[] arguments)
    {
        return await RunCliInDirectory(workingDirectory, environment: null, arguments);
    }

    protected static async Task<(int ExitCode, string Stdout)> RunCliWithGitHubEnvironment(string workingDirectory, string summaryPath, params string[] arguments)
    {
        return await RunCliInDirectory(
            workingDirectory,
            new Dictionary<string, string?>
            {
                ["GITHUB_ACTIONS"] = "true",
                ["GITHUB_STEP_SUMMARY"] = summaryPath,
                ["GITHUB_WORKSPACE"] = workingDirectory
            },
            arguments);
    }

    protected static async Task<(int ExitCode, string Stdout)> RunCliInDirectory(string workingDirectory, IReadOnlyDictionary<string, string?>? environment, params string[] arguments)
    {
        ProcessStartInfo psi = new()
        {
            FileName = GetCliPath(),
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        RemoveGitHubActionsEnvironment(psi);
        ApplyEnvironment(psi, environment);

        foreach (string argument in arguments)
        {
            psi.ArgumentList.Add(argument);
        }

        using Process? process = Process.Start(psi);
        Assert.That(process, Is.Not.Null);

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        string stdout = await stdoutTask;
        string stderr = await stderrTask;

        return (process.ExitCode, stdout + stderr);
    }

    protected static void RemoveGitHubActionsEnvironment(ProcessStartInfo psi)
    {
        psi.Environment.Remove("GITHUB_ACTIONS");
        psi.Environment.Remove("GITHUB_STEP_SUMMARY");
        psi.Environment.Remove("GITHUB_WORKSPACE");
    }

    protected static void ApplyEnvironment(ProcessStartInfo psi, IReadOnlyDictionary<string, string?>? environment)
    {
        if (environment is null) return;

        foreach ((string key, string? value) in environment)
        {
            if (value is null)
            {
                psi.Environment.Remove(key);
            }
            else
            {
                psi.Environment[key] = value;
            }
        }
    }

    protected static string RunGit(string workingDirectory, string arguments)
    {
        ProcessStartInfo psi = new()
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process? process = Process.Start(psi);
        Assert.That(process, Is.Not.Null);

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        string stdout = stdoutTask.GetAwaiter().GetResult();
        string stderr = stderrTask.GetAwaiter().GetResult();

        Assert.That(process.ExitCode, Is.Zero, stderr);
        return stdout;
    }

    protected static string CreateGitRepoWithInitialCommit(string workingDirectory, params (string RelativePath, string Contents)[] files)
    {
        RunGit(workingDirectory, "init");
        RunGit(workingDirectory, "config user.email \"test@example.com\"");
        RunGit(workingDirectory, "config user.name \"Test User\"");
        RunGit(workingDirectory, "config commit.gpgsign false");
        RunGit(workingDirectory, "config core.autocrlf false");

        foreach ((string relativePath, string contents) in files)
        {
            WriteTextFile(workingDirectory, relativePath, contents);
        }

        RunGit(workingDirectory, "add .");
        RunGit(workingDirectory, "commit -m \"Initial\"");
        return RunGit(workingDirectory, "rev-parse HEAD").Trim();
    }

    protected static void WriteTextFile(string rootDirectory, string relativePath, string contents)
    {
        string path = Path.Combine(rootDirectory, relativePath);
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, contents);
    }

    protected static string CreateCoverageXml(string sourceDirectory, string fileName)
    {
        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
              <sources>
                <source>{sourceDirectory}</source>
              </sources>
              <packages>
                <package name="package-1">
                  <classes>
                    <class name="class-1" filename="{fileName}">
                      <methods/>
                      <lines>
                        <line number="1" hits="1"/>
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;
    }

    protected static string CreateCoverageXml(string sourceDirectory, string fileName, params (int Number, int Hits)[] lines)
    {
        string lineElements = string.Join(
            Environment.NewLine,
            lines.Select(line => $"                        <line number=\"{line.Number}\" hits=\"{line.Hits}\"/>"));

        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
              <sources>
                <source>{sourceDirectory}</source>
              </sources>
              <packages>
                <package name="package-1">
                  <classes>
                    <class name="class-1" filename="{fileName}">
                      <methods/>
                      <lines>
            {lineElements}
                      </lines>
                    </class>
                  </classes>
                </package>
              </packages>
            </coverage>
            """;
    }

    protected static string CreateCoverageXml(string sourceDirectory, params (string FileName, (int Number, int Hits)[] Lines)[] files)
    {
        string classElements = string.Join(
            Environment.NewLine,
            files.Select((file, index) =>
            {
                string lineElements = string.Join(
                    Environment.NewLine,
                    file.Lines.Select(line => $"                        <line number=\"{line.Number}\" hits=\"{line.Hits}\"/>"));

                return string.Join(
                    Environment.NewLine,
                    $"                    <class name=\"class-{index + 1}\" filename=\"{file.FileName}\">",
                    "                      <methods/>",
                    "                      <lines>",
                    lineElements,
                    "                      </lines>",
                    "                    </class>");
            }));

        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
              <sources>
                <source>{sourceDirectory}</source>
              </sources>
              <packages>
                <package name="package-1">
                  <classes>
            {classElements}
                  </classes>
                </package>
              </packages>
            </coverage>
            """;
    }

    protected static string CreateChangedClass(string value)
    {
        return string.Join(
            Environment.NewLine,
            "public class Changed",
            "{",
            $"    public int Value => {value};",
            "}",
            string.Empty);
    }

    protected sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
            System.IO.Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (System.IO.Directory.Exists(Path))
                {
                    foreach (string file in System.IO.Directory.GetFiles(Path, "*", SearchOption.AllDirectories))
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                    }

                    System.IO.Directory.Delete(Path, true);
                }
            }
            catch
            {
                // Ignore cleanup failures from transient file locks.
            }
        }
    }
}
