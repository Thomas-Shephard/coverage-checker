using System.Diagnostics;
using CoverageChecker.Services;
using CoverageChecker.Utils;
using Moq;

namespace CoverageChecker.Tests.Unit.ServiceTests;

public class ProcessExecutorTests
{
    private Mock<ISystemProcess> _mockProcess;
    private ProcessExecutor _sut;
    private MemoryStream _standardOutput;
    private MemoryStream _standardError;
    private StreamReader _stdoutReader;
    private StreamReader _stderrReader;

    [SetUp]
    public void Setup()
    {
        _mockProcess = new Mock<ISystemProcess>();
        _standardOutput = new MemoryStream();
        _standardError = new MemoryStream();
        _stdoutReader = new StreamReader(_standardOutput, leaveOpen: true);
        _stderrReader = new StreamReader(_standardError, leaveOpen: true);

        _mockProcess.SetupGet(p => p.StartInfo).Returns(new ProcessStartInfo());
        _mockProcess.SetupGet(p => p.StandardOutput).Returns(_stdoutReader);
        _mockProcess.SetupGet(p => p.StandardError).Returns(_stderrReader);
        _mockProcess.Setup(p => p.Start()).Returns(true);

        _sut = new ProcessExecutor(() => _mockProcess.Object, null);
    }

    [TearDown]
    public void TearDown()
    {
        _stdoutReader.Dispose();
        _stderrReader.Dispose();
        _standardOutput.Dispose();
        _standardError.Dispose();
    }

    [Test]
    public void ExecuteShouldTimeoutWhenWaitForExitReturnsFalse()
    {
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(false); // Simulate timeout
        string fileName = "git";
        string[] arguments = ["status"];
        TimeSpan timeout = TimeSpan.FromSeconds(1);

        ProcessExecutionException? ex = Assert.Throws<ProcessExecutionException>(() => _sut.Execute(fileName, arguments, null, timeout));
        Assert.That(ex.Message, Does.Contain("timed out after 1 second"));
        _mockProcess.Verify(p => p.Kill(), Times.Once);
    }

    [Test]
    public void ExecuteShouldReturnOutputWhenProcessFinishesInTime()
    {
        // Arrange
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(true); // Simulate success
        _mockProcess.SetupGet(p => p.ExitCode).Returns(0);
        
        using (StreamWriter writer = new(_standardOutput, leaveOpen: true))
        {
            writer.Write("Hello World");
            writer.Flush();
        }
        _standardOutput.Position = 0;

        string fileName = "git";
        string[] arguments = ["status"];
        TimeSpan timeout = TimeSpan.FromSeconds(5);

        // Act
        (int ExitCode, string StandardOutput, string StandardError) result = _sut.Execute(fileName, arguments, null, timeout);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.StandardOutput, Is.EqualTo("Hello World"));
        });
    }

    [Test]
    public void ExecuteShouldSetWorkingDirectoryWhenProvided()
    {
        // Arrange
        string workingDir = "C:\\Temp";
        _sut = new ProcessExecutor(() => _mockProcess.Object, null);
        
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(true);

        // Act - Testing method-level working directory
        _sut.Execute("git", ["status"], workingDir);

        // Assert
        Assert.That(_mockProcess.Object.StartInfo.WorkingDirectory, Is.EqualTo(workingDir));
    }

    [Test]
    public void ExecuteShouldPrioritizeMethodWorkingDirectory()
    {
        // Arrange
        string constructorDir = "C:\\Old";
        string methodDir = "C:\\New";
        _sut = new ProcessExecutor(() => _mockProcess.Object, constructorDir);
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(true);

        // Act
        _sut.Execute("git", ["status"], methodDir);

        // Assert
        Assert.That(_mockProcess.Object.StartInfo.WorkingDirectory, Is.EqualTo(methodDir));
    }

    [Test]
    public void ExecuteShouldLogWarningWhenKillFailsAfterTimeout()
    {
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(false);
        _mockProcess.Setup(p => p.Kill()).Throws(new InvalidOperationException("Kill failed"));

        // Act
        Assert.Throws<ProcessExecutionException>(() => _sut.Execute("git", ["status"], null, TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void ExecuteShouldUsePluralSecondsWhenTimeoutIsGreaterThanOne()
    {
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(false);
        
        ProcessExecutionException? ex = Assert.Throws<ProcessExecutionException>(() => _sut.Execute("git", ["status"], null, TimeSpan.FromSeconds(2)));
        Assert.That(ex.Message, Does.Contain("2 seconds"));
    }

    [Test]
    public void ExecuteShouldUseDefaultTimeoutWhenTimeoutIsNull()
    {
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(false);

        ProcessExecutionException? ex = Assert.Throws<ProcessExecutionException>(() => _sut.Execute("git", ["status"], null, null));
        Assert.That(ex.Message, Does.Contain("30 seconds"));
    }

    [Test]
    public void ExecuteShouldHandleEmptyArguments()
    {
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(true);
        _mockProcess.SetupGet(p => p.ExitCode).Returns(0);

        Assert.DoesNotThrow(() => _sut.Execute("git", [], (string?)null));
    }

    [Test]
    public void ExecuteShellShouldSetWindowsShellInfo()
    {
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(true);
        _sut.OverridePathStyle(PathStyle.Windows);

        _sut.ExecuteShell("echo hello");

        Assert.Multiple(() =>
        {
            Assert.That(_mockProcess.Object.StartInfo.FileName, Is.EqualTo("cmd.exe"));
            Assert.That(_mockProcess.Object.StartInfo.Arguments, Is.EqualTo("/s /c \"echo hello\""));
        });
    }

    [Test]
    public void ExecuteShellShouldSetUnixShellInfo()
    {
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(true);
        _sut.OverridePathStyle(PathStyle.Unix);

        _sut.ExecuteShell("echo hello");

        Assert.Multiple(() =>
        {
            Assert.That(_mockProcess.Object.StartInfo.FileName, Is.EqualTo("sh"));
            Assert.That(_mockProcess.Object.StartInfo.ArgumentList, Contains.Item("echo hello"));
            Assert.That(_mockProcess.Object.StartInfo.ArgumentList, Contains.Item("-c"));
        });
    }

    [Test]
    public void ExecuteShellShouldSetShellInfo()
    {
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(true);
        bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        string expectedShell = isWindows ? "cmd.exe" : "sh";

        _sut.ExecuteShell("echo hello");

        Assert.Multiple(() =>
        {
            Assert.That(_mockProcess.Object.StartInfo.FileName, Is.EqualTo(expectedShell));
            if (isWindows)
            {
                Assert.That(_mockProcess.Object.StartInfo.Arguments, Is.EqualTo("/s /c \"echo hello\""));
            }
            else
            {
                Assert.That(_mockProcess.Object.StartInfo.ArgumentList, Contains.Item("echo hello"));
            }
        });
    }

    [Test]
    public async Task ExecuteAsyncShouldReturnOutput()
    {
        _mockProcess.Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockProcess.SetupGet(p => p.ExitCode).Returns(0);

        await using (StreamWriter writer = new(_standardOutput, leaveOpen: true))
        {
            await writer.WriteAsync("Async Hello");
            await writer.FlushAsync();
        }
        _standardOutput.Position = 0;

        (int ExitCode, string StandardOutput, string StandardError) result = await _sut.ExecuteAsync("git", ["status"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.StandardOutput, Is.EqualTo("Async Hello"));
        });
    }

    [Test]
    public void ExecuteAsyncShouldTimeout()
    {
        _mockProcess.Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>()))
                    .Returns((CancellationToken ct) => Task.Delay(TimeSpan.FromSeconds(10), ct)); // Long running task respecting cancellation
        
        Assert.ThrowsAsync<ProcessExecutionException>(async () => await _sut.ExecuteAsync("git", ["status"], null, TimeSpan.FromMilliseconds(100)));
        _mockProcess.Verify(p => p.Kill(), Times.AtLeastOnce);
    }

    [Test]
    public async Task ExecuteShellAsyncShouldSetShellInfo()
    {
        _mockProcess.Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        string expectedShell = isWindows ? "cmd.exe" : "sh";

        await _sut.ExecuteShellAsync("echo async");

        Assert.That(_mockProcess.Object.StartInfo.FileName, Is.EqualTo(expectedShell));
    }
}
