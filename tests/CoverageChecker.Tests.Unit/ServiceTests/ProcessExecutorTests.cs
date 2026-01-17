using System.Diagnostics;
using CoverageChecker.Services;
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
    public void Execute_ShouldTimeout_WhenWaitForExitReturnsFalse()
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
    public void Execute_ShouldReturnOutput_WhenProcessFinishesInTime()
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
    public void Execute_ShouldSetWorkingDirectory_WhenProvided()
    {
        // Arrange
        string workingDir = "C:\\Temp";
        _sut = new ProcessExecutor(() => _mockProcess.Object, null);
        
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(true);

        // Act - Testing method-level working directory
        _sut.Execute("git", ["status"], workingDir, null);

        // Assert
        Assert.That(_mockProcess.Object.StartInfo.WorkingDirectory, Is.EqualTo(workingDir));
    }

    [Test]
    public void Execute_ShouldPrioritizeMethodWorkingDirectory()
    {
        // Arrange
        string constructorDir = "C:\\Old";
        string methodDir = "C:\\New";
        _sut = new ProcessExecutor(() => _mockProcess.Object, constructorDir);
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(true);

        // Act
        _sut.Execute("git", ["status"], methodDir, null);

        // Assert
        Assert.That(_mockProcess.Object.StartInfo.WorkingDirectory, Is.EqualTo(methodDir));
    }

    [Test]
    public void Execute_ShouldLogWarning_WhenKillFailsAfterTimeout()
    {
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(false);
        _mockProcess.Setup(p => p.Kill()).Throws(new Exception("Kill failed"));

        // Act
        Assert.Throws<ProcessExecutionException>(() => _sut.Execute("git", ["status"], null, TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void Execute_ShouldUsePluralSeconds_WhenTimeoutIsGreaterThanOne()
    {
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(false);
        
        ProcessExecutionException? ex = Assert.Throws<ProcessExecutionException>(() => _sut.Execute("git", ["status"], null, TimeSpan.FromSeconds(2)));
        Assert.That(ex.Message, Does.Contain("2 seconds"));
    }

    [Test]
    public void Execute_ShouldUseDefaultTimeout_WhenTimeoutIsNull()
    {
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(false);

        ProcessExecutionException? ex = Assert.Throws<ProcessExecutionException>(() => _sut.Execute("git", ["status"], null, null));
        Assert.That(ex.Message, Does.Contain("30 seconds"));
    }
}