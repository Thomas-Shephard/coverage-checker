using CoverageChecker.Services;

namespace CoverageChecker.Tests.Unit.ServiceTests;

public class ProcessExecutorTests
{
    private ProcessExecutor _sut;

    [SetUp]
    public void Setup()
    {
        _sut = new ProcessExecutor();
    }

    [Test]
    public void Execute_ShouldTimeout_WhenProcessRunsTooLong()
    {
        // Use powershell to sleep for 2 seconds
        string fileName = "powershell";
        string[] arguments = ["-Command", "Start-Sleep -Seconds 2"];
        TimeSpan timeout = TimeSpan.FromSeconds(1);

        GitException? ex = Assert.Throws<GitException>(() => _sut.Execute(fileName, arguments, timeout));
        Assert.That(ex.Message, Does.Contain("timed out after 1 seconds"));
    }

    [Test]
    public void Execute_ShouldReturnOutput_WhenProcessFinishesInTime()
    {
        string fileName = "powershell";
        string[] arguments = ["-Command", "Write-Output 'Hello World'"];
        TimeSpan timeout = TimeSpan.FromSeconds(5);

        (int ExitCode, string StandardOutput, string StandardError) result = _sut.Execute(fileName, arguments, timeout);

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.StandardOutput.Trim(), Is.EqualTo("Hello World"));
        });
    }
}