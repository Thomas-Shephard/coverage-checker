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

    [SetUp]
    public void Setup()
    {
        _mockProcess = new Mock<ISystemProcess>();
        _standardOutput = new MemoryStream();
        _standardError = new MemoryStream();

        _mockProcess.SetupGet(p => p.StartInfo).Returns(new ProcessStartInfo());
        _mockProcess.SetupGet(p => p.StandardOutput).Returns(new StreamReader(_standardOutput));
        _mockProcess.SetupGet(p => p.StandardError).Returns(new StreamReader(_standardError));
        _mockProcess.Setup(p => p.Start()).Returns(true);

        _sut = new ProcessExecutor(() => _mockProcess.Object, null);
    }

    [TearDown]
    public void TearDown()
    {
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

        ProcessExecutionException? ex = Assert.Throws<ProcessExecutionException>(() => _sut.Execute(fileName, arguments, timeout));
        Assert.That(ex.Message, Does.Contain("timed out after 1 second"));
        _mockProcess.Verify(p => p.Kill(), Times.Once);
    }

    [Test]
    public void Execute_ShouldReturnOutput_WhenProcessFinishesInTime()
    {
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(true);
        _mockProcess.SetupGet(p => p.ExitCode).Returns(0);

        StreamWriter writer = new(_standardOutput);
        writer.Write("Hello World");
        writer.Flush();
        _standardOutput.Position = 0;

        string fileName = "git";
        string[] arguments = ["status"];
        TimeSpan timeout = TimeSpan.FromSeconds(5);

        (int ExitCode, string StandardOutput, string StandardError) result = _sut.Execute(fileName, arguments, timeout);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.StandardOutput, Is.EqualTo("Hello World"));
        });
    }

    [Test]
    public void Execute_ShouldSetWorkingDirectory_WhenProvided()
    {
        string workingDir = "C:\\Temp";
        _sut = new ProcessExecutor(() => _mockProcess.Object, workingDir);
        
        _mockProcess.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(true);

        _sut.Execute("git", ["status"]);

        Assert.That(_mockProcess.Object.StartInfo.WorkingDirectory, Is.EqualTo(workingDir));
    }
}