namespace CoverageChecker.EndToEndTests;

public class CoverageAnalyserInvalidTests {
    [Test]
    public void CoverageAnalyser_AnalyseCoverage_InvalidCoverageFormat_ThrowsException() {
        string directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "CoverageFiles", "Cobertura");
        const string globPattern = "FullLineCoverage.xml";

        CoverageAnalyser coverageAnalyser = new((CoverageFormat)5, directory, globPattern);

        Exception e = Assert.Throws<ArgumentOutOfRangeException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo($"Unknown coverage format (Parameter 'coverageFormat'){Environment.NewLine}Actual value was 5."));
    }
}