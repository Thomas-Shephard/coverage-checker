namespace CoverageChecker.CommandLine.Tests.Unit;

public class CommandLineOptionsTests
{
    [Test]
    public void Options_WhenNoValuesSet_HasDefaultValues()
    {
        CommandLineOptions options = new();

        Assert.Multiple(() =>
        {
            Assert.That(options.Directory, Is.EqualTo(Environment.CurrentDirectory));
            Assert.That(options.GlobPatterns, Is.EqualTo(new[] { "*.xml" }));
            Assert.That(options.LineThreshold, Is.EqualTo(0.8));
            Assert.That(options.BranchThreshold, Is.EqualTo(0.8));
        });
    }

    [TestCase(0)]
    [TestCase(50)]
    [TestCase(100)]
    public void LineThreshold_WhenSetToValidValues_IsSet(double value)
    {
        CommandLineOptions options = new() { LineThreshold = value };

        Assert.That(options.LineThreshold, Is.EqualTo(value / 100));
    }
    
    [TestCase(-1)]
    [TestCase(101)]
    public void LineThreshold_WhenSetToInvalidValues_ThrowsException(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new CommandLineOptions { LineThreshold = value });
    }
    
    [TestCase(0)]
    [TestCase(50)]
    [TestCase(100)]
    public void BranchThreshold_WhenSetToValidValues_IsSet(double value)
    {
        CommandLineOptions options = new() { BranchThreshold = value };

        Assert.That(options.BranchThreshold, Is.EqualTo(value / 100));
    }
    
    [TestCase(-1)]
    [TestCase(101)]
    public void BranchThreshold_WhenSetToInvalidValues_ThrowsException(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new CommandLineOptions { BranchThreshold = value });
    }
}