using CoverageChecker.Results;
using CoverageChecker.Utils;

namespace CoverageChecker.Tests.UtilTests.CoverageUtilTests;

public class CoverageUtilTests {
    [Test]
    public void CalculateCoverageUtils_Invalid_FileCoverage_ThrowsException() {
        FileCoverage[] files = [
            new FileCoverage([
                new LineCoverage(1, true),
                new LineCoverage(2, true)
            ], "coverage-file-1"),
            new FileCoverage([
                new LineCoverage(1, true),
                new LineCoverage(2, true),
                new LineCoverage(3, true)
            ], "coverage-file-2")
        ];

        Assert.Throws<ArgumentOutOfRangeException>(() => files.CalculateCoverage((CoverageType) 2));
    }

    [Test]
    public void CalculateCoverageUtils_Invalid_LineCoverage_ThrowsException() {
        LineCoverage[] lines = [
            new LineCoverage(1, true),
            new LineCoverage(2, true),
            new LineCoverage(3, true),
            new LineCoverage(4, true),
            new LineCoverage(5, true)
        ];

        Assert.Throws<ArgumentOutOfRangeException>(() => lines.CalculateCoverage((CoverageType) 3));
    }
}