using CoverageChecker.Results;

namespace CoverageChecker.UnitTests.ResultTests;

public class LineCoverageTests {
    [TestCase(1, false, null, null, CoverageTestData.ClassName, CoverageTestData.MethodName, CoverageTestData.MethodSignature)]
    [TestCase(2, false, 1, 0, CoverageTestData.ClassName, null, null)]
    [TestCase(1, false, 3, 0, null, CoverageTestData.MethodName, CoverageTestData.MethodSignature)]
    [TestCase(2, true, 3, 2, null, CoverageTestData.MethodName, null)]
    [TestCase(5, true, 3, 3, null, null, null)]
    public void LineCoverage_ConstructorValid_ReturnsObject(int lineNumber, bool isCovered, int? branches, int? coveredBranches, string? className, string? methodName, string? methodSignature) {
        LineCoverage lineCoverage = new(lineNumber, isCovered, branches, coveredBranches, className, methodName, methodSignature);

        Assert.Multiple(() => {
            Assert.That(lineCoverage.LineNumber, Is.EqualTo(lineNumber));
            Assert.That(lineCoverage.IsCovered, Is.EqualTo(isCovered));
            Assert.That(lineCoverage.Branches, Is.EqualTo(branches));
            Assert.That(lineCoverage.CoveredBranches, Is.EqualTo(coveredBranches));
            Assert.That(lineCoverage.ClassName, Is.EqualTo(className));
            Assert.That(lineCoverage.MethodName, Is.EqualTo(methodName));
            Assert.That(lineCoverage.MethodSignature, Is.EqualTo(methodSignature));
        });
    }

    [TestCase(null, 0)]
    [TestCase(1, null)]
    public void LineCoverage_ConstructorWithMismatchedNullabilityBetweenBranchesAndCoveredBranches_ThrowsException(int? branches, int? coveredBranches) {
        Exception e = Assert.Throws<ArgumentException>(() => _ = new LineCoverage(1, true, branches, coveredBranches));
        Assert.That(e.Message, Is.EqualTo("The branch coverage information is invalid, either both branches and covered branches are null, or both are not null"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(-10)]
    public void LineCoverage_ConstructorWithNonPositiveBranches_ThrowsException(int branches) {
        Exception e = Assert.Throws<ArgumentException>(() => _ = new LineCoverage(1, true, branches, 0));
        Assert.That(e.Message, Is.EqualTo("The branch coverage information is invalid, there must be at least one branch and zero covered branches"));
    }

    [TestCase(-1)]
    [TestCase(-5)]
    [TestCase(-10)]
    public void LineCoverage_ConstructorWithNegativeCoveredBranches_ThrowsException(int coveredBranches) {
        Exception e = Assert.Throws<ArgumentException>(() => _ = new LineCoverage(1, true, 1, coveredBranches));
        Assert.That(e.Message, Is.EqualTo("The branch coverage information is invalid, there must be at least one branch and zero covered branches"));
    }

    [TestCase(2, 3)]
    [TestCase(3, 5)]
    [TestCase(1, 10)]
    public void LineCoverage_ConstructorWithCoveredBranchesGreaterThanBranches_ThrowsException(int branches, int coveredBranches) {
        Exception e = Assert.Throws<ArgumentException>(() => _ = new LineCoverage(1, true, branches, coveredBranches));
        Assert.That(e.Message, Is.EqualTo("The branch coverage information is invalid, there cannot be more covered branches than branches"));
    }

    [Test]
    public void LineCoverage_ConstructorWithMethodSignatureWithoutMethodName_ThrowsException() {
        Exception e = Assert.Throws<ArgumentException>(() => _ = new LineCoverage(1, true, methodName: null, methodSignature: CoverageTestData.MethodSignature));
        Assert.That(e.Message, Is.EqualTo("The method signature cannot be set without the method name"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void LineCoverage_CalculateLineCoverage_LineCoverage_ReturnsCoverage(bool isCovered) {
        LineCoverage lineCoverage = new(1, isCovered);

        double coverage = lineCoverage.CalculateLineCoverage();

        Assert.That(coverage, Is.EqualTo(isCovered ? 1 : 0));
    }

    [TestCase(3, 2)]
    [TestCase(5, 3)]
    [TestCase(10, 5)]
    public void LineCoverage_CalculateLineCoverage_BranchCoverage_ReturnsCoverage(int branches, int coveredBranches) {
        LineCoverage lineCoverage = new(1, true, branches, coveredBranches);

        double coverage = lineCoverage.CalculateLineCoverage(CoverageType.Branch);

        Assert.That(coverage, Is.EqualTo((double)coveredBranches / branches));
    }

    [Test]
    public void LineCoverage_EquivalentTo_SameObject_ReturnsTrue() {
        LineCoverage lineCoverage = new(1, false);

        Assert.That(lineCoverage.EquivalentTo(lineCoverage), Is.True);
    }

    [Test]
    public void LineCoverage_EquivalentTo_EqualObjects_ReturnsTrue() {
        LineCoverage lineCoverage1 = new(2, false);
        LineCoverage lineCoverage2 = new(2, false);

        Assert.That(lineCoverage1.EquivalentTo(lineCoverage2), Is.True);
    }

    [Test]
    public void LineCoverage_EquivalentTo_Null_ReturnsFalse() {
        LineCoverage lineCoverage = new(1, true);

        Assert.That(lineCoverage.EquivalentTo(null), Is.False);
    }

    [Test]
    public void LineCoverage_EquivalentTo_DifferentObjects_ReturnsFalse() {
        LineCoverage lineCoverage1 = new(1, true);
        LineCoverage lineCoverage2 = new(2, false);

        Assert.That(lineCoverage1.EquivalentTo(lineCoverage2), Is.False);
    }
}