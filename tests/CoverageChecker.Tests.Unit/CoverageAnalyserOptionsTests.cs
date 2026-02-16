namespace CoverageChecker.Tests.Unit;

public class CoverageAnalyserOptionsTests
{
    [TestCase(0.8)]
    [TestCase(0.0)]
    [TestCase(1.0)]
    public void RenameThresholdValidValueSetsProperty(double threshold)
    {
        CoverageAnalyserOptions options = new() { RenameThreshold = threshold };
        Assert.That(options.RenameThreshold, Is.EqualTo(threshold));
    }

    [TestCase(-0.1)]
    [TestCase(1.1)]
    [TestCase(double.NaN)]
    public void RenameThresholdInvalidValuesThrowsException(double threshold)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new CoverageAnalyserOptions { RenameThreshold = threshold });
    }

    [Test]
    public void DefaultValuesAreCorrect()
    {
        CoverageAnalyserOptions options = new();
        
        Assert.Multiple(() =>
        {
            Assert.That(options.RenameThreshold, Is.EqualTo(0.5));
            Assert.That(options.CoverageFormat, Is.EqualTo(CoverageFormat.Auto));
            Assert.That(options.Directory, Is.EqualTo(Environment.CurrentDirectory));
            Assert.That(options.GlobPatterns, Is.EquivalentTo((string[])["**/*.xml"]));
            Assert.That(options.Include, Is.Null);
            Assert.That(options.Exclude, Is.Null);
        });
    }
}
