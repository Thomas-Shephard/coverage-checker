using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CoverageChecker.Tests.EndToEnd;

public class CommandLineRunTests
{
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
        string configuration = new DirectoryInfo(baseDir).Name;
        string? exePath = Directory.GetFiles(artifactsDir, exeName, SearchOption.AllDirectories)
                                   .FirstOrDefault(f => f.Contains(configuration) && !f.Contains("publish"));

        // Fallback to any match if configuration-specific one isn't found
        exePath ??= Directory.GetFiles(artifactsDir, exeName, SearchOption.AllDirectories)
                                .FirstOrDefault(f => !f.Contains("publish"));

        return exePath ?? throw new InvalidOperationException($"Could not find {exeName} executable in {artifactsDir}. Set COVERAGE_CHECKER_CLI_PATH to override.");
    }

    [Test]
    public async Task RunCommandExecutesAndAttemptsAnalysis()
    {
        string cliPath = GetCliPath();
        
        string command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "echo dummy > {output}\\coverage.xml"
            : "echo dummy > {output}/coverage.xml";
        
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
        
        using Process? process = Process.Start(psi);
        Assert.That(process, Is.Not.Null);
        
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        string stdout = await stdoutTask;
        await stderrTask;
        
        Assert.Multiple(() => {
            Assert.That(process.ExitCode, Is.Not.Zero);
            Assert.That(stdout, Does.Contain("No coverage files found."));
        });
    }
}
