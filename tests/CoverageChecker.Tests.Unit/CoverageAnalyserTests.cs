namespace CoverageChecker.Tests.Unit;

public class CoverageAnalyserTests {
    private const CoverageFormat ValidCoverageFormat = CoverageFormat.SonarQube;
    private const string ValidDirectory = ".";

    [Test]
    public void CoverageAnalyserConstructor_ValidGlobPattern_DoesNotThrow() {
        Assert.DoesNotThrow(() => _ = new CoverageAnalyser(ValidCoverageFormat, ValidDirectory, "*.xml"));
    }

    [Test]
    public void CoverageAnalyserConstructor_ValidGlobPatterns_DoesNotThrow() {
        Assert.DoesNotThrow(() => _ = new CoverageAnalyser(ValidCoverageFormat, ValidDirectory, ["file1.xml", "file2.xml"]));
    }

    [Test]
    public void CoverageAnalyserConstructor_EmptyGlobPatterns_ThrowsException() {
        Exception e = Assert.Throws<ArgumentException>(() => _ = new CoverageAnalyser(ValidCoverageFormat, ValidDirectory, []));
        Assert.That(e.Message, Is.EqualTo("At least one glob pattern must be provided"));
    }
}