using CoverageChecker.Results;

namespace CoverageChecker.Tests.Unit.ResultTests;

public class LineCoverageComparer : IEqualityComparer<LineCoverage>
{
    public bool Equals(LineCoverage? x, LineCoverage? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.LineNumber == y.LineNumber && x.IsCovered == y.IsCovered && x.Branches == y.Branches && x.CoveredBranches == y.CoveredBranches && x.ClassName == y.ClassName && x.MethodName == y.MethodName && x.MethodSignature == y.MethodSignature;
    }

    public int GetHashCode(LineCoverage obj)
    {
        return HashCode.Combine(obj.LineNumber, obj.IsCovered, obj.Branches, obj.CoveredBranches, obj.ClassName, obj.MethodName, obj.MethodSignature);
    }
}