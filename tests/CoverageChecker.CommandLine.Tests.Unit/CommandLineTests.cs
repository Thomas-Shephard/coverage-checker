
namespace CoverageChecker.CommandLine.Tests.Unit;

[TestFixture]
public class CommandLineTests
{
    private StringWriter _outputWriter = null!;
    private StringWriter _errorWriter = null!;
    private App _app = null!;

    [SetUp]
    public void Setup()
    {
        _outputWriter = new StringWriter();
        _errorWriter = new StringWriter();
        _app = new App(_outputWriter, _errorWriter);
    }

    [TearDown]
    public void TearDown()
    {
        _outputWriter.Dispose();
        _errorWriter.Dispose();
    }

    [Test]
    public void Run_WithNoArguments_ShowsHelp()
    {
        var exitCode = _app.Run([]);

        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(1));
            Assert.That(_outputWriter.ToString(), Does.Contain("help"));
            Assert.That(_errorWriter.ToString(), Is.Empty);
        });
    }
}