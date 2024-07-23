using CoverageChecker.Results;

namespace CoverageChecker.EndToEndTests;

public class CoverageAnalyserSonarQubeTests {
    private readonly string _directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "CoverageFiles", "SonarQube");

    [Test]
    public void CoverageAnalyser_AnalyseSonarQubeCoverage_FullLineCoverage_ReturnsCoverage() {
        Coverage coverage = new CoverageAnalyser(CoverageFormat.SonarQube, _directory, "FullLineCoverage.xml").AnalyseCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Has.Count.EqualTo(4));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(4));
            Assert.That(coverage.Files[2].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[3].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo((double)5 / 6));
        });
    }

    [Test]
    public void CoverageAnalyser_AnalyseSonarQubeCoverage_FullBranchCoverage_ReturnsCoverage() {
        Coverage coverage = new CoverageAnalyser(CoverageFormat.SonarQube, _directory, "FullBranchCoverage.xml").AnalyseCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.Files[2].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo(1));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.EqualTo(1));
        });
    }

    [Test]
    public void CoverageAnalyser_AnalyseSonarQubeCoverage_PartialLineCoverage_ReturnsCoverage() {
        Coverage coverage = new CoverageAnalyser(CoverageFormat.SonarQube, _directory, "PartialLineCoverage.xml").AnalyseCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[0].Lines, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[1].Lines, Has.Count.EqualTo(3));
            Assert.That(coverage.CalculateOverallCoverage(), Is.EqualTo((double)1 / 5));
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void CoverageAnalyser_AnalyseSonarQubeCoverage_NoFiles_ReturnsCoverage() {
        Coverage coverage = new CoverageAnalyser(CoverageFormat.SonarQube, _directory, "NoFiles.xml").AnalyseCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void CoverageAnalyser_AnalyseSonarQubeCoverage_NoLines_ReturnsCoverage() {
        Coverage coverage = new CoverageAnalyser(CoverageFormat.SonarQube, _directory, "NoLines.xml").AnalyseCoverage();

        Assert.Multiple(() => {
            Assert.That(coverage.Files, Has.Count.EqualTo(2));
            Assert.That(coverage.Files[0].Lines, Is.Empty);
            Assert.That(coverage.Files[1].Lines, Is.Empty);
            Assert.That(coverage.CalculateOverallCoverage(), Is.NaN);
            Assert.That(coverage.CalculateOverallCoverage(CoverageType.Branch), Is.NaN);
        });
    }

    [Test]
    public void CoverageAnalyser_AnalyseSonarQubeCoverage_InvalidVersion_ThrowsCoverageParseException() {
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.SonarQube, _directory, "InvalidVersion.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Attribute 'version' on element 'coverage' must be '1'"));
    }

    [Test]
    public void CoverageAnalyser_AnalyseSonarQubeCoverage_EmptyFile_ThrowsCoverageParseException() {
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.SonarQube, _directory, "EmptyFile.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("Failed to load coverage file"));
    }

    [Test]
    public void CoverageAnalyser_AnalyseSonarQubeCoverage_InvalidFile_ThrowsCoverageParseException() {
        CoverageAnalyser coverageAnalyser = new(CoverageFormat.SonarQube, _directory, "InvalidFile.xml");

        Exception e = Assert.Throws<CoverageParseException>(() => coverageAnalyser.AnalyseCoverage());
        Assert.That(e.Message, Is.EqualTo("No coverage files found"));
    }
}