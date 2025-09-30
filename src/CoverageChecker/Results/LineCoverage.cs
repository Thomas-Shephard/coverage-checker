using CoverageChecker.Utils;

namespace CoverageChecker.Results;

/// <summary>
/// Represents coverage information for a single line within a file.
/// </summary>
public class LineCoverage : ICoverageResult
{
    /// <summary>
    /// The line number.
    /// </summary>
    public int LineNumber { get; }

    /// <summary>
    /// Whether the line is covered.
    /// If true, the line has been covered, otherwise, false.
    /// </summary>
    public bool IsCovered { get; private set; }

    /// <summary>
    /// The number of branches in the line.
    /// If null, the line does not have any branches.
    /// </summary>
    public int? Branches { get; private set; }

    /// <summary>
    /// The number of covered branches in the line.
    /// If null, the line does not have any branches.
    /// </summary>
    public int? CoveredBranches { get; private set; }

    /// <summary>
    /// The name of the class the line is part of.
    /// If null, the line is not part of a class.
    /// </summary>
    public string? ClassName { get; }

    /// <summary>
    /// The name of the method the line is part of.
    /// If null, the line is not part of a method.
    /// </summary>
    public string? MethodName { get; }

    /// <summary>
    /// The method signature of the method the line is part of.
    /// If null, the line is not part of a method or the method does not have a method signature.
    /// </summary>
    public string? MethodSignature { get; }

    public IReadOnlyList<LineCoverage> Lines => [this];

    internal LineCoverage(int lineNumber, bool isCovered, int? branches = null, int? coveredBranches = null, string? className = null, string? methodName = null, string? methodSignature = null)
    {
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

    /// <summary>
    /// Calculates the coverage for this line.
    /// </summary>
    /// <param name="coverageType">The type of coverage to calculate. Defaults to <see cref="CoverageType.Line"/>.</param>
    /// <returns>The coverage for this line.</returns>
    public double CalculateLineCoverage(CoverageType coverageType = CoverageType.Line)
    {
        LineCoverage[] lines = [this];

        return lines.CalculateCoverage(coverageType);
    }

    internal void MergeWith(LineCoverage other)
    {
        if (!MergeRequired(other))
            return;

        IsCovered = IsCovered || other.IsCovered;
        Branches ??= other.Branches;

        if (Branches is not null)
        {
            CoveredBranches = Math.Max(CoveredBranches ?? 0, other.CoveredBranches ?? 0);
        }
    }

    private bool MergeRequired(LineCoverage other)
    {
        if (ReferenceEquals(this, other)) return false;

        // The line numbers and class names should always be the same
        if (LineNumber != other.LineNumber)
            throw new CoverageParseException("Cannot merge lines due to a line number mismatch");
        if (ClassName != other.ClassName)
            throw new CoverageParseException("Cannot merge lines due to a class name mismatch");

        // If the updateable information is the same, no merge is required
        if (IsCovered == other.IsCovered && Branches == other.Branches && CoveredBranches == other.CoveredBranches)
            return false;

        // If the branches are the same, no additional checks are required and a merge can be performed
        // Otherwise, branches can only be updated from null when the line was previously not covered but now is
        if (Branches == other.Branches || (Branches is null && !IsCovered && other.IsCovered))
            return true;

        // If the other branches is null and was not covered and the line was previously not covered, this is valid but
        // no update is required because it could only decrease code coverage
        if (other.Branches is null && !other.IsCovered && IsCovered)
            return false;

        throw new CoverageParseException("Cannot merge lines due to a branches mismatch");
    }
}