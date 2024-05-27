using CoverageChecker.Results;

namespace CoverageChecker.Tests.ResultTests;

public class LineCoverageTests {
    private const int LineNumber = 1;
    private const bool IsCovered = true;

    [TestCase(null, null)]
    [TestCase(1, 0)]
    [TestCase(3, 0)]
    [TestCase(3, 2)]
    [TestCase(3, 3)]
    public void LineCoverage_BranchesAndCoveredBranchesMatchNullability_ReturnsObject(int? branches, int? coveredBranches) {
        LineCoverage lineCoverage = new(LineNumber, IsCovered, branches, coveredBranches);

        Assert.That(lineCoverage.LineNumber, Is.EqualTo(LineNumber));
        Assert.That(lineCoverage.IsCovered, Is.EqualTo(IsCovered));
        Assert.That(lineCoverage.Branches, Is.EqualTo(branches));
        Assert.That(lineCoverage.CoveredBranches, Is.EqualTo(coveredBranches));
    }

    [TestCase(null, 0)]
    [TestCase(1, null)]
    public void LineCoverage_BranchesAndCoveredBranchesMismatchNullability_ThrowsArgumentException(int? branches, int? coveredBranches) {
        Assert.Throws<ArgumentException>(() => _ = new LineCoverage(LineNumber, IsCovered, branches, coveredBranches));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(-10)]
    public void LineCoverage_NonPositiveBranches_ThrowsArgumentException(int branches) {
        Assert.Throws<ArgumentException>(() => _ = new LineCoverage(LineNumber, IsCovered, branches, 0));
    }

    [TestCase(-1)]
    [TestCase(-5)]
    [TestCase(-10)]
    public void LineCoverage_NegativeCoveredBranches_ThrowsArgumentException(int coveredBranches) {
        Assert.Throws<ArgumentException>(() => _ = new LineCoverage(LineNumber, IsCovered, 1, coveredBranches));
    }

    [TestCase(2, 3)]
    [TestCase(3, 5)]
    [TestCase(1, 10)]
    public void LineCoverage_CoveredBranchesGreaterThanBranches_ThrowsArgumentException(int branches, int coveredBranches) {
        Assert.Throws<ArgumentException>(() => _ = new LineCoverage(LineNumber, IsCovered, branches, coveredBranches));
    }
}