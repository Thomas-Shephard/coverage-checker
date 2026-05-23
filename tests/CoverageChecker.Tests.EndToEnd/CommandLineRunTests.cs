using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CoverageChecker.Tests.EndToEnd;

public class CommandLineRunTests
{
    private static readonly string CoberturaCoverageFiles = Path.Combine(TestContext.CurrentContext.TestDirectory, "CoverageFiles", "Cobertura");

    private static string GetCliPath()
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

        // Try to find the executable in a directory that matches the current configuration (Debug/Release)
        // baseDir example: .../bin/Debug/net8.0
        string configName = new DirectoryInfo(baseDir).Parent?.Name ?? "Debug";
        string? exePath = Directory.GetFiles(artifactsDir, exeName, SearchOption.AllDirectories)
                                   .FirstOrDefault(f => f.Contains(configName) && !f.Contains("publish"));

        // Fallback to any match if configuration-specific one isn't found
        exePath ??= Directory.GetFiles(artifactsDir, exeName, SearchOption.AllDirectories)
                                .FirstOrDefault(f => !f.Contains("publish"));

        return exePath ?? throw new InvalidOperationException($"Could not find {exeName} executable in {artifactsDir}. Set COVERAGE_CHECKER_CLI_PATH to override.");
    }

    [Test]
    public async Task RunCommandExecutesAndAttemptsAnalysis()
    {
        string cliPath = GetCliPath();
        
        string command = "echo dummy > {output}/coverage.xml";
        
        ProcessStartInfo psi = new()
        {
            FileName = cliPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--command");
        psi.ArgumentList.Add(command);
        psi.ArgumentList.Add("--continue-on-failure");
        
        using Process? process = Process.Start(psi);
        Assert.That(process, Is.Not.Null);
        
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        string stdout = await stdoutTask;
        await stderrTask;
        
        // We expect it to find the file but fail to parse it
        // Note: With log level Warning, we won't see "Found 1 coverage files" in stdout
        // but we will see the failure message.
        Assert.That(stdout, Does.Contain("Error parsing coverage files."));
    }

    [Test]
    public async Task RunCommandFailsWhenNoFilesFound()
    {
        string cliPath = GetCliPath();
        
        // Command that doesn't create any files
        string command = "echo hello";
        
        ProcessStartInfo psi = new()
        {
            FileName = cliPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--command");
        psi.ArgumentList.Add(command);
        psi.ArgumentList.Add("--continue-on-failure");
        
        using Process? process = Process.Start(psi);
        Assert.That(process, Is.Not.Null);
        
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        string stdout = await stdoutTask;
        await stderrTask;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(process.ExitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("No coverage files found."));
        }
    }

    [Test]
    public async Task CheckCommandFailsWhenCoverageFileParsesNoFiles()
    {
        using TestDirectory testDirectory = new();
        File.Copy(Path.Combine(CoberturaCoverageFiles, "NoPackages.xml"), Path.Combine(testDirectory.Path, "coverage.xml"));

        (int exitCode, string stdout) = await RunCli("check", "--directory", testDirectory.Path, "--glob-patterns", "coverage.xml", "--format", "Cobertura");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Parsed coverage information for 0 files."));
            Assert.That(stdout, Does.Contain("Line coverage could not be calculated because no applicable lines were found."));
        }
    }

    [Test]
    public async Task CheckCommandFailsWhenParsedFilesHaveNoLines()
    {
        using TestDirectory testDirectory = new();
        File.Copy(Path.Combine(CoberturaCoverageFiles, "NoLines.xml"), Path.Combine(testDirectory.Path, "coverage.xml"));

        (int exitCode, string stdout) = await RunCli("check", "--directory", testDirectory.Path, "--glob-patterns", "coverage.xml", "--format", "Cobertura");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Parsed coverage information for 2 files."));
            Assert.That(stdout, Does.Contain("Line coverage could not be calculated because no applicable lines were found."));
        }
    }

    [Test]
    public async Task CheckCommandFailsWhenFiltersRemoveAllFiles()
    {
        using TestDirectory testDirectory = new();
        File.Copy(Path.Combine(CoberturaCoverageFiles, "FullLineCoverage.xml"), Path.Combine(testDirectory.Path, "coverage.xml"));

        (int exitCode, string stdout) = await RunCli("check", "--directory", testDirectory.Path, "--glob-patterns", "coverage.xml", "--format", "Cobertura", "--include", "does-not-match/**");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Parsed coverage information for 0 files."));
            Assert.That(stdout, Does.Contain("Line coverage could not be calculated because no applicable lines were found."));
        }
    }

    [Test]
    public async Task CheckCommandPassesWhenLineCoverageMeetsThresholdAndNoBranchesExist()
    {
        using TestDirectory testDirectory = new();
        string coverageXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
              <packages>
                <package name="package-1">
                  <classes>
                    <class name="class-1" filename="file-1">
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
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), coverageXml);

        (int exitCode, string stdout) = await RunCli("check", "--directory", testDirectory.Path, "--glob-patterns", "coverage.xml", "--format", "Cobertura", "--line-threshold", "100", "--branch-threshold", "100");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Zero);
            Assert.That(stdout, Does.Contain("Overall branch coverage: NaN."));
            Assert.That(stdout, Does.Contain("The coverage threshold has been met."));
        }
    }

    [Test]
    public async Task CheckCommandFailsWhenDeltaChangedLinesAreMissingFromCoverage()
    {
        using TestDirectory testDirectory = new();
        RunGit(testDirectory.Path, "init");
        RunGit(testDirectory.Path, "config user.email \"test@example.com\"");
        RunGit(testDirectory.Path, "config user.name \"Test User\"");
        RunGit(testDirectory.Path, "config commit.gpgsign false");
        RunGit(testDirectory.Path, "config core.autocrlf false");

        string changedFile = Path.Combine(testDirectory.Path, "Changed.cs");
        File.WriteAllText(changedFile, "public class Changed\n{\n}\n");
        RunGit(testDirectory.Path, "add .");
        RunGit(testDirectory.Path, "commit -m \"Initial\"");
        string baseCommit = RunGit(testDirectory.Path, "rev-parse HEAD").Trim();

        File.WriteAllText(changedFile, "public class Changed\n{\n    public void Method() {}\n}\n");
        RunGit(testDirectory.Path, "add Changed.cs");
        RunGit(testDirectory.Path, "commit -m \"Update changed file\"");

        string coverageXml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
              <sources>
                <source>{testDirectory.Path}</source>
              </sources>
              <packages>
                <package name="package-1">
                  <classes>
                    <class name="class-1" filename="Changed.cs">
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
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), coverageXml);

        (int exitCode, string stdout) = await RunCliInDirectory(testDirectory.Path, "check", "--directory", testDirectory.Path, "--glob-patterns", "coverage.xml", "--format", "Cobertura", "--delta", "--delta-base", baseCommit);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("Git reported changed lines, but none were found in the coverage data."));
            Assert.That(stdout, Does.Not.Contain("No changed lines found for delta coverage."));
        }
    }

    [Test]
    public async Task CheckCommandPassesWhenOnlyDeltaChangesAreAbsentFromCoverageFiles()
    {
        using TestDirectory testDirectory = new();
        RunGit(testDirectory.Path, "init");
        RunGit(testDirectory.Path, "config user.email \"test@example.com\"");
        RunGit(testDirectory.Path, "config user.name \"Test User\"");
        RunGit(testDirectory.Path, "config commit.gpgsign false");
        RunGit(testDirectory.Path, "config core.autocrlf false");

        string readme = Path.Combine(testDirectory.Path, "README.md");
        File.WriteAllText(readme, "# Project\n");
        RunGit(testDirectory.Path, "add README.md");
        RunGit(testDirectory.Path, "commit -m \"Initial\"");
        string baseCommit = RunGit(testDirectory.Path, "rev-parse HEAD").Trim();

        File.WriteAllText(readme, "# Project\n\nDocs update.\n");
        RunGit(testDirectory.Path, "add README.md");
        RunGit(testDirectory.Path, "commit -m \"Update docs\"");

        string coverageXml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage>
              <sources>
                <source>{testDirectory.Path}</source>
              </sources>
              <packages>
                <package name="package-1">
                  <classes>
                    <class name="class-1" filename="Covered.cs">
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
        File.WriteAllText(Path.Combine(testDirectory.Path, "Covered.cs"), "public class Covered {}\n");
        File.WriteAllText(Path.Combine(testDirectory.Path, "coverage.xml"), coverageXml);

        (int exitCode, string stdout) = await RunCliInDirectory(testDirectory.Path, "check", "--directory", testDirectory.Path, "--glob-patterns", "coverage.xml", "--format", "Cobertura", "--delta", "--delta-base", baseCommit);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exitCode, Is.Zero);
            Assert.That(stdout, Does.Contain("No changed lines found for delta coverage."));
            Assert.That(stdout, Does.Not.Contain("Git reported changed lines, but none were found in the coverage data."));
        }
    }

    private static async Task<(int ExitCode, string Stdout)> RunCli(params string[] arguments)
    {
        return await RunCliInDirectory(Environment.CurrentDirectory, arguments);
    }

    private static async Task<(int ExitCode, string Stdout)> RunCliInDirectory(string workingDirectory, params string[] arguments)
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

    private static string RunGit(string workingDirectory, string arguments)
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
        process.WaitForExit();
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        Assert.That(process.ExitCode, Is.Zero, stderr);
        return stdout;
    }

    private sealed class TestDirectory : IDisposable
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
