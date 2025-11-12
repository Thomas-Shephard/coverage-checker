namespace CoverageChecker.Results;

public interface ICoverageResult
{
    IEnumerable<LineCoverage> GetLines();
}