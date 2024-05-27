namespace CoverageChecker.Results;

public class LineCoverage {
    public bool IsCovered { get; }
    public int? Branches { get; }
    public int? CoveredBranches { get; }

    public LineCoverage(bool isCovered, int? branches = null, int? coveredBranches = null) {
        // Either both branches and coveredBranches are null, or both are not null
        if ((branches is null && coveredBranches is not null) || (branches is not null && coveredBranches is null))
            throw new ArgumentException("The branch coverage information is invalid, either both branches and covered branches are null, or both are not null");
        // If not null, the branches must be at least 1, and the coveredBranches must be at least 0
        if (branches < 1 || coveredBranches < 0)
            throw new ArgumentException("The branch coverage information is invalid, there must be at least one branch and zero covered branches");
        // The coveredBranches must be less than or equal to the branches
        if (coveredBranches > branches)
            throw new ArgumentException("The branch coverage information is invalid, there cannot be more covered branches than branches");

        IsCovered = isCovered;
        Branches = branches;
        CoveredBranches = coveredBranches;
    }
}