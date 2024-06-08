using CoverageChecker.Results;

namespace CoverageChecker.Tests.ResultTests;

public class LineCoverageTests {
    private const int LineNumber = 1;
    private const bool IsCovered = true;

    [TestCase(null, null, "class-name", "method-name", "method-signature")]
    [TestCase(1, 0, "class-name", null, null)]
    [TestCase(3, 0, null, "method-name", "method-signature")]
    [TestCase(3, 2, null, "method-name", null)]
    [TestCase(3, 3, null, null, null)]
    public void LineCoverage_BranchesAndCoveredBranchesMatchNullability_ReturnsObject(int? branches, int? coveredBranches, string? className, string? methodName, string? methodSignature) {
        LineCoverage lineCoverage = new(LineNumber, IsCovered, branches, coveredBranches, className, methodName, methodSignature);

        Assert.Multiple(() => {
            Assert.That(lineCoverage.LineNumber, Is.EqualTo(LineNumber));
            Assert.That(lineCoverage.IsCovered, Is.EqualTo(IsCovered));
            Assert.That(lineCoverage.Branches, Is.EqualTo(branches));
            Assert.That(lineCoverage.CoveredBranches, Is.EqualTo(coveredBranches));
            Assert.That(lineCoverage.ClassName, Is.EqualTo(className));
            Assert.That(lineCoverage.MethodName, Is.EqualTo(methodName));
            Assert.That(lineCoverage.MethodSignature, Is.EqualTo(methodSignature));
        });
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

    [Test]
    public void LineCoverage_MethodSignatureWithoutMethodName_ThrowsArgumentException() {
        Assert.Throws<ArgumentException>(() => _ = new LineCoverage(LineNumber, IsCovered, methodName: null, methodSignature: "method-signature"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void LineCoverage_CalculateLineCoverage_Line_ReturnsCoverage(bool isCovered) {
        LineCoverage lineCoverage = new(LineNumber, isCovered);

        double coverage = lineCoverage.CalculateLineCoverage();

        Assert.That(coverage, Is.EqualTo(isCovered ? 1 : 0));
    }

    [TestCase(3, 2)]
    [TestCase(5, 3)]
    [TestCase(10, 5)]
    public void LineCoverage_CalculateLineCoverage_Branch_ReturnsCoverage(int branches, int coveredBranches) {
        LineCoverage lineCoverage = new(LineNumber, IsCovered, branches, coveredBranches);

        double coverage = lineCoverage.CalculateLineCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo((double)coveredBranches / branches));
    }

    [Test]
    public void LineCoverage_EquivalentTo_SameObject_ReturnsTrue() {
        LineCoverage lineCoverage = new(LineNumber, IsCovered);

        Assert.That(lineCoverage.EquivalentTo(lineCoverage), Is.True);
    }

    [Test]
    public void LineCoverage_EquivalentTo_NullObject_ReturnsFalse() {
        LineCoverage lineCoverage = new(LineNumber, IsCovered);

        Assert.That(lineCoverage.EquivalentTo(null), Is.False);
    }

    [Test]
    public void LineCoverage_EquivalentTo_Different_ReturnsFalse() {
        LineCoverage lineCoverage1 = new(LineNumber, true);
        LineCoverage lineCoverage2 = new(LineNumber, false);

        Assert.That(lineCoverage1.EquivalentTo(lineCoverage2), Is.False);
    }

    [Test]
    public void LineCoverage_EquivalentTo_Same_ReturnsTrue() {
        LineCoverage lineCoverage1 = new(LineNumber, IsCovered);
        LineCoverage lineCoverage2 = new(LineNumber, IsCovered);

        Assert.That(lineCoverage1.EquivalentTo(lineCoverage2), Is.True);
    }
}