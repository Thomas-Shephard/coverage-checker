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
    public void Merge_SameObject_ReturnsSameObject()
    {
        LineCoverage lineCoverage = new(1, true);

        _service.Merge(lineCoverage, lineCoverage);

        Assert.That(lineCoverage.IsCovered, Is.True);
    }

    [Test]
    public void Merge_DifferentIsCovered_ReturnsMergedObject()
    {
        LineCoverage firstLineCoverage = new(1, false);
        LineCoverage secondLineCoverage = new(1, true);
        LineCoverage expectedLineCoverage = new(1, true);

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void Merge_DifferentIsCovered_ReturnsMergedObjectReverse()
    {
        LineCoverage firstLineCoverage = new(1, true);
        LineCoverage secondLineCoverage = new(1, false);
        LineCoverage expectedLineCoverage = new(1, true);

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void Merge_DifferentCoveredBranches_ReturnsMergedObject()
    {
        LineCoverage firstLineCoverage = new(1, true, 2, 1);
        LineCoverage secondLineCoverage = new(1, true, 2, 2);
        LineCoverage expectedLineCoverage = new(1, true, 2, 2);

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void Merge_DifferentCoveredBranches_ReturnsMergedObjectReverse()
    {
        LineCoverage firstLineCoverage = new(1, true, 2, 1);
        LineCoverage secondLineCoverage = new(1, true, 2, 0);
        LineCoverage expectedLineCoverage = new(1, true, 2, 1);

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void Merge_DifferentBranches_ReturnsMergedObject()
    {
        LineCoverage firstLineCoverage = new(1, false);
        LineCoverage secondLineCoverage = new(1, true, 2, 2);
        LineCoverage expectedLineCoverage = new(1, true, 2, 2);

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void Merge_DifferentBranches_ReturnsMergedObjectReverse()
    {
        LineCoverage firstLineCoverage = new(1, true, 2, 1);
        LineCoverage secondLineCoverage = new(1, false);
        LineCoverage expectedLineCoverage = new(1, true, 2, 1);

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void Merge_DifferentMetadata_ReturnsMergedObject()
    {
        LineCoverage firstLineCoverage = new(1, true, className: "ClassName");
        LineCoverage secondLineCoverage = new(1, true, methodName: "MethodName", methodSignature: "MethodSignature");
        LineCoverage expectedLineCoverage = new(1, true, className: "ClassName", methodName: "MethodName", methodSignature: "MethodSignature");

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void Merge_MetadataUpdateWhenBranchLogicWouldSkip_ReturnsMergedObject()
    {
        // Existing is covered and has branches
        LineCoverage firstLineCoverage = new(1, true, 2, 1, className: "ClassName");
        // Incoming is NOT covered and has no branches, but has a MethodName
        LineCoverage secondLineCoverage = new(1, false, methodName: "MethodName");
        
        LineCoverage expectedLineCoverage = new(1, true, 2, 1, className: "ClassName", methodName: "MethodName");

        _service.Merge(firstLineCoverage, secondLineCoverage);

        Assert.That(firstLineCoverage, Is.EqualTo(expectedLineCoverage).Using(new LineCoverageComparer()));
    }

    [Test]
    public void Merge_DifferentLineNumbers_ThrowsCoverageParseException()
    {
        LineCoverage firstLineCoverage = new(1, true);
        LineCoverage secondLineCoverage = new(2, true);

        Exception e = Assert.Throws<CoverageParseException>(() => _service.Merge(firstLineCoverage, secondLineCoverage));
        Assert.That(e.Message, Is.EqualTo("Cannot merge lines due to a line number mismatch"));
    }

    [Test]
    public void Merge_DifferentClassNames_ThrowsCoverageParseException()
    {
        LineCoverage firstLineCoverage = new(1, true, className: "ClassName1");
        LineCoverage secondLineCoverage = new(1, true, className: "ClassName2");

        Exception e = Assert.Throws<CoverageParseException>(() => _service.Merge(firstLineCoverage, secondLineCoverage));
        Assert.That(e.Message, Is.EqualTo("Cannot merge lines due to a class name mismatch"));
    }

    [Test]
    public void Merge_InvalidDifferentBranches1_ThrowsCoverageParseException([Values] bool firstIsCovered, [Values] bool secondIsCovered)
    {
        LineCoverage firstLineCoverage = new(1, firstIsCovered, 2, 0);
        LineCoverage secondLineCoverage = new(1, secondIsCovered, 4, 0);

        Exception e = Assert.Throws<CoverageParseException>(() => _service.Merge(firstLineCoverage, secondLineCoverage));
        Assert.That(e.Message, Is.EqualTo("Cannot merge lines due to a branches mismatch"));
    }

    [Test]
    public void Merge_InvalidDifferentBranches2_ThrowsCoverageParseException([Values] bool firstIsCovered)
    {
        LineCoverage firstLineCoverage = new(1, firstIsCovered, 2, 0);
        LineCoverage secondLineCoverage = new(1, true);

        Exception e = Assert.Throws<CoverageParseException>(() => _service.Merge(firstLineCoverage, secondLineCoverage));
        Assert.That(e.Message, Is.EqualTo("Cannot merge lines due to a branches mismatch"));
    }

    [Test]
    public void Merge_InvalidDifferentBranches3_ThrowsCoverageParseException([Values] bool secondIsCovered)
    {
        LineCoverage firstLineCoverage = new(1, true);
        LineCoverage secondLineCoverage = new(1, secondIsCovered, 4, 0);

        Exception e = Assert.Throws<CoverageParseException>(() => _service.Merge(firstLineCoverage, secondLineCoverage));
        Assert.That(e.Message, Is.EqualTo("Cannot merge lines due to a branches mismatch"));
    }
}