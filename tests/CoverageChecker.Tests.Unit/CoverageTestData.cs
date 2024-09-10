using CoverageChecker.Results;

namespace CoverageChecker.Tests.Unit;

internal static class CoverageTestData {
    internal const string FilePath = "file-path";
    internal const string PackageName = "package-name";
    internal const string ClassName = "class-name";
    internal const string MethodName = "method-name";
    internal const string MethodSignature = "method-signature";

    internal static readonly FileCoverage[] FilesEmpty = [];
    internal static readonly LineCoverage[] LinesEmpty = [];

    internal static readonly LineCoverage[] Lines0Of3Covered = [
        new LineCoverage(1, false),
        new LineCoverage(2, false),
        new LineCoverage(3, false),
    ];

    internal static readonly LineCoverage[] Lines3Of5Covered = [
        new LineCoverage(1, false),
        new LineCoverage(2, true),
        new LineCoverage(3, true),
        new LineCoverage(4, false),
        new LineCoverage(5, true)
    ];

    internal static readonly LineCoverage[] Lines4Of4Covered = [
        new LineCoverage(1, true),
        new LineCoverage(2, true),
        new LineCoverage(3, true),
        new LineCoverage(4, true),
    ];

    internal static readonly LineCoverage[] Lines1Of1CoveredWith0Of4Branches = [
        new LineCoverage(1, true, 4, 0)
    ];

    internal static readonly LineCoverage[] Lines2Of3CoveredWith3Of4Branches = [
        new LineCoverage(1, true, 4, 3),
        new LineCoverage(2, true),
        new LineCoverage(3, false)
    ];

    internal static readonly LineCoverage[] Lines3Of3CoveredWith2Of2Branches = [
        new LineCoverage(1, true, 2, 2),
        new LineCoverage(2, true),
        new LineCoverage(3, true)
    ];
}