using System.Collections.ObjectModel;
using CoverageChecker.Services;
using CoverageChecker.Utils;

namespace CoverageChecker.Results;

/// <summary>
/// Represents coverage information for a single file.
/// </summary>
public class FileCoverage : ICoverageResult
{
    /// <summary>
    /// The path of the file.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The name of the package the file is part of.
    /// If null, the file is not part of a package.
    /// </summary>
    public string? PackageName { get; }

    public IReadOnlyList<LineCoverage> Lines => _lines.AsReadOnly();

    private readonly LineCoverageCollection _lines = [];

    internal FileCoverage(string path, string? packageName = null)
    {
        Path = path;
        PackageName = packageName;
    }

    /// <summary>
    /// Adds a line to the file coverage or merges it if it already exists.
    /// </summary>
    /// <param name="incomingLine">The line to add or merge.</param>
    /// <param name="mergeService">The service to use for merging.</param>
    internal void AddOrMergeLine(LineCoverage incomingLine, ICoverageMergeService mergeService)
    {
        if (_lines.TryGetValue(incomingLine.LineNumber, out LineCoverage? existingLine))
        {
            mergeService.Merge(existingLine, incomingLine);
        }
        else
        {
            _lines.Add(incomingLine);
        }
    }

    /// <summary>
    /// Calculates the coverage for the file.
    /// </summary>
    /// <param name="coverageType">The type of coverage to calculate. Defaults to <see cref="CoverageType.Line"/>.</param>
    /// <returns>The coverage for the file.</returns>
    public double CalculateFileCoverage(CoverageType coverageType = CoverageType.Line)
    {
        return _lines.CalculateCoverage(coverageType);
    }

    /// <summary>
    /// Calculates the coverage for all lines that are part of the specified class.
    /// </summary>
    /// <param name="className">The name of the class to filter by.</param>
    /// <param name="coverageType">The type of coverage to calculate. Defaults to <see cref="CoverageType.Line"/>.</param>
    /// <returns>The coverage for all lines that are part of the specified class.</returns>
    /// <exception cref="CoverageCalculationException">Thrown when no lines are found that are part of the specified class.</exception>
    public double CalculateClassCoverage(string className, CoverageType coverageType = CoverageType.Line)
    {
        LineCoverage[] filteredLines = _lines.Where(line => line.ClassName == className)
                                             .ToArray();

        if (filteredLines.Length is 0)
            throw new CoverageCalculationException("No lines found for the specified class name");

        return filteredLines.CalculateCoverage(coverageType);
    }

    /// <summary>
    /// Calculates the coverage for all lines that are part of the specified method.
    /// </summary>
    /// <param name="methodName">The name of the method to filter by.</param>
    /// <param name="methodSignature">
    /// Optionally, the signature of the method to filter by. If null, only the method name is
    /// checked.
    /// </param>
    /// <param name="coverageType">The type of coverage to calculate. Defaults to <see cref="CoverageType.Line"/>.</param>
    /// <returns>The coverage for all lines that are part of the specified method.</returns>
    /// <exception cref="CoverageCalculationException">Thrown when no lines are found that are part of the specified method.</exception>
    public double CalculateMethodCoverage(string methodName, string? methodSignature = null, CoverageType coverageType = CoverageType.Line)
    {
        LineCoverage[] filteredLines = _lines.Where(line =>
                                             {
                                                 // If the method signature is null, only the method name is checked
                                                 if (methodSignature is null)
                                                     return line.MethodName == methodName;
                                                 return line.MethodName == methodName && line.MethodSignature == methodSignature;
                                             })
                                             .ToArray();

        if (filteredLines.Length is 0)
            throw new CoverageCalculationException("No lines found for the specified method");

        return filteredLines.CalculateCoverage(coverageType);
    }

    private sealed class LineCoverageCollection : KeyedCollection<int, LineCoverage>
    {
        protected override int GetKeyForItem(LineCoverage item) => item.LineNumber;
    }
}