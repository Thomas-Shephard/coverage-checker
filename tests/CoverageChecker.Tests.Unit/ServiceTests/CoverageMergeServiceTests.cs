using CoverageChecker.Results;
using CoverageChecker.Services;
using CoverageChecker.Tests.Unit.ResultTests;

namespace CoverageChecker.Tests.Unit.ServiceTests;

public class CoverageMergeServiceTests
{
    private CoverageMergeService _service;

    [SetUp]
    public void Setup()
    {
        _service = new CoverageMergeService();
    }

    [Test]
    public void MergeSameObjectDoesNotModify()
    {
        LineCoverage lineCoverage = new(1, true);

        _service.Merge(lineCoverage, lineCoverage);

        Assert.That(lineCoverage.IsCovered, Is.True);
    }

    [Test]
    public void MergeIdenticalDataDifferentObjectsDoesNotModify()
    {
        LineCoverage firstLineCoverage = new(1, true, 2, 1, "Class", "Method", "Signature");
        LineCoverage secondLineCoverage = new(1, true, 2, 1, "Class", "Method", "Signature");
        LineCoverage expectedLineCoverage = new(1, true, 2, 1, "Class", "Method", "Signature");

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void MergeDifferentIsCoveredUpdatesExistingLine()
    {
        LineCoverage firstLineCoverage = new(1, false);
        LineCoverage secondLineCoverage = new(1, true);
        LineCoverage expectedLineCoverage = new(1, true);

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void MergeDifferentIsCoveredUpdatesExistingLineReverse()
    {
        LineCoverage firstLineCoverage = new(1, true);
        LineCoverage secondLineCoverage = new(1, false);
        LineCoverage expectedLineCoverage = new(1, true);

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void MergeDifferentCoveredBranchesUpdatesExistingLine()
    {
        LineCoverage firstLineCoverage = new(1, true, 2, 1);
        LineCoverage secondLineCoverage = new(1, true, 2, 2);
        LineCoverage expectedLineCoverage = new(1, true, 2, 2);

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void MergeDifferentCoveredBranchesUpdatesExistingLineReverse()
    {
        LineCoverage firstLineCoverage = new(1, true, 2, 1);
        LineCoverage secondLineCoverage = new(1, true, 2, 0);
        LineCoverage expectedLineCoverage = new(1, true, 2, 1);

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void MergeDifferentBranchesUpdatesExistingLine()
    {
        LineCoverage firstLineCoverage = new(1, false);
        LineCoverage secondLineCoverage = new(1, true, 2, 2);
        LineCoverage expectedLineCoverage = new(1, true, 2, 2);

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void MergeDifferentBranchesCoveredUpdatesExistingLine()
    {
        LineCoverage firstLineCoverage = new(1, true);
        LineCoverage secondLineCoverage = new(1, true, 2, 2);
        LineCoverage expectedLineCoverage = new(1, true, 2, 2);

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void MergeDifferentBranchesUpdatesExistingLineReverse()
    {
        LineCoverage firstLineCoverage = new(1, true, 2, 1);
        LineCoverage secondLineCoverage = new(1, false);
        LineCoverage expectedLineCoverage = new(1, true, 2, 1);

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void MergeDifferentLineNumbersThrowsCoverageParseException()
    {
        LineCoverage firstLineCoverage = new(1, true);
        LineCoverage secondLineCoverage = new(2, true);

        Exception e = Assert.Throws<CoverageParseException>(() => _service.Merge(firstLineCoverage, secondLineCoverage));
        Assert.That(e.Message, Is.EqualTo("Cannot merge lines due to a line number mismatch"));
    }

    [Test]
    public void MergeDifferentClassNamesThrowsCoverageParseException()
    {
        LineCoverage firstLineCoverage = new(1, true, className: "ClassName1");
        LineCoverage secondLineCoverage = new(1, true, className: "ClassName2");

        Exception e = Assert.Throws<CoverageParseException>(() => _service.Merge(firstLineCoverage, secondLineCoverage));
        Assert.That(e.Message, Is.EqualTo("Cannot merge lines due to a class name mismatch"));
    }

    [Test]
    public void MergeDifferentMethodNamesThrowsCoverageParseException()
    {
        LineCoverage firstLineCoverage = new(1, true, methodName: "MethodName1");
        LineCoverage secondLineCoverage = new(1, true, methodName: "MethodName2");

        Exception e = Assert.Throws<CoverageParseException>(() => _service.Merge(firstLineCoverage, secondLineCoverage));
        Assert.That(e.Message, Is.EqualTo("Cannot merge lines due to a method name mismatch"));
    }

    [Test]
    public void MergeDifferentMethodSignaturesThrowsCoverageParseException()
    {
        LineCoverage firstLineCoverage = new(1, true, methodName: "MethodName", methodSignature: "MethodSignature1");
        LineCoverage secondLineCoverage = new(1, true, methodName: "MethodName", methodSignature: "MethodSignature2");

        Exception e = Assert.Throws<CoverageParseException>(() => _service.Merge(firstLineCoverage, secondLineCoverage));
        Assert.That(e.Message, Is.EqualTo("Cannot merge lines due to a method signature mismatch"));
    }

    [Test]
    public void MergeOneClassNameNullDoesNotThrow()
    {
        LineCoverage firstLineCoverage = new(1, true);
        LineCoverage secondLineCoverage = new(1, true, className: "ClassName2");
        LineCoverage expectedLineCoverage = new(1, true, className: "ClassName2");

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void MergeMetadataUpdatesExistingLine()
    {
        LineCoverage firstLineCoverage = new(1, true);
        LineCoverage secondLineCoverage = new(1, true, className: "Class", methodName: "Method", methodSignature: "Signature");
        LineCoverage expectedLineCoverage = new(1, true, className: "Class", methodName: "Method", methodSignature: "Signature");

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void MergeInvalidDifferentBranches1ThrowsCoverageParseException([Values] bool firstIsCovered, [Values] bool secondIsCovered)
    {
        LineCoverage firstLineCoverage = new(1, firstIsCovered, 2, 0);
        LineCoverage secondLineCoverage = new(1, secondIsCovered, 4, 0);

        Exception e = Assert.Throws<CoverageParseException>(() => _service.Merge(firstLineCoverage, secondLineCoverage));
        Assert.That(e.Message, Is.EqualTo("Cannot merge lines due to a branches mismatch"));
    }

    [Test]
    public void MergeInvalidDifferentBranches2ThrowsCoverageParseException([Values] bool firstIsCovered)
    {
        LineCoverage firstLineCoverage = new(1, firstIsCovered, 2, 0);
        LineCoverage secondLineCoverage = new(1, true);

        Exception e = Assert.Throws<CoverageParseException>(() => _service.Merge(firstLineCoverage, secondLineCoverage));
        Assert.That(e.Message, Is.EqualTo("Cannot merge lines due to a branches mismatch"));
    }
}