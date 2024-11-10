using CoverageChecker.Results;

namespace CoverageChecker.Tests.Unit;

internal static class CoverageTestData
{
    internal const string FilePath = "file-path";
    internal const string PackageName = "package-name";
    internal const string ClassName = "class-name";
    internal const string MethodName = "method-name";
    internal const string MethodSignature = "method-signature";

    internal static readonly FileCoverage[] FilesEmpty = [];
    internal static readonly LineCoverage[] LinesEmpty = [];

    internal static readonly LineCoverage[] Lines0Of3Covered =
    [
        new(1, false),
        new(2, false),
        new(3, false)
    ];

    internal static readonly LineCoverage[] Lines3Of5Covered =
    [
        new(1, false),
        new(2, true),
        new(3, true),
        new(4, false),
        new(5, true)
    ];

    internal static readonly LineCoverage[] Lines4Of4Covered =
    [
        new(1, true),
        new(2, true),
        new(3, true),
        new(4, true)
    ];

    internal static readonly LineCoverage[] Lines1Of1CoveredWith0Of4Branches =
    [
        new(1, true, 4, 0)
    ];

    internal static readonly LineCoverage[] Lines2Of3CoveredWith3Of4Branches =
    [
        new(1, true, 4, 3),
        new(2, true),
        new(3, false)
    ];

    internal static readonly LineCoverage[] Lines3Of3CoveredWith2Of2Branches =
    [
        new(1, true, 2, 2),
        new(2, true),
        new(3, true)
    ];
}