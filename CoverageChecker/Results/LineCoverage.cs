using CoverageChecker.Utils;

namespace CoverageChecker.Results;

public class LineCoverage {
    public int LineNumber { get; }
    public bool IsCovered { get; }
    public int? Branches { get; }
    public int? CoveredBranches { get; }
    public string? ClassName { get; }
    public string? MethodName { get; }
    public string? MethodSignature { get; }

    public LineCoverage(int lineNumber, bool isCovered, int? branches = null, int? coveredBranches = null, string? className = null, string? methodName = null, string? methodSignature = null) {
        // Either both branches and coveredBranches are null, or both are not null
        if ((branches is null && coveredBranches is not null) || (branches is not null && coveredBranches is null))
            throw new ArgumentException("The branch coverage information is invalid, either both branches and covered branches are null, or both are not null");
        // If not null, the branches must be at least 1, and the coveredBranches must be at least 0
        if (branches < 1 || coveredBranches < 0)
            throw new ArgumentException("The branch coverage information is invalid, there must be at least one branch and zero covered branches");
        // The coveredBranches must be less than or equal to the branches
        if (coveredBranches > branches)
            throw new ArgumentException("The branch coverage information is invalid, there cannot be more covered branches than branches");
        // If the method signature is not null, the method name must not be null
        if (methodSignature is not null && methodName is null)
            throw new ArgumentException("The method signature cannot be set without the method name");

        LineNumber = lineNumber;
        IsCovered = isCovered;
        Branches = branches;
        CoveredBranches = coveredBranches;
        ClassName = className;
        MethodName = methodName;
        MethodSignature = methodSignature;
    }

    public double CalculateLineCoverage(CoverageType coverageType = CoverageType.Line) {
        return CoverageUtils.CalculateCoverage([this], coverageType);
    }

    internal bool EquivalentTo(LineCoverage? other) {
        // Checks if the lines are the same (excluding method name and signature)
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return LineNumber == other.LineNumber &&
               IsCovered == other.IsCovered &&
               Branches == other.Branches &&
               CoveredBranches == other.CoveredBranches &&
               ClassName == other.ClassName;
    }
}