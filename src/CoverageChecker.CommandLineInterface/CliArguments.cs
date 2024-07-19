﻿using CommandLine;

namespace CoverageChecker.CommandLineInterface;

public class CliArguments {
    [Option('f', "format", Default = CoverageFormat.Cobertura, HelpText = "The format of the coverage file(s). Either 'Cobertura' or 'SonarQube'.")]
    public CoverageFormat Format { get; set; }

    [Option('d', "directory", HelpText = "(Default: Current Directory) The directory to use to search for coverage file(s) in.")]
    public string Directory { get; set; } = Environment.CurrentDirectory;

    [Option('g', "glob-patterns", Default = new[] { "*.xml" }, HelpText = "The glob pattern(s) to use to search for the coverage file(s).")]
    public IEnumerable<string> GlobPatterns {
        get => _globPatterns;
        set {
            if (!value.Any())
                throw new ArgumentException("At least one glob pattern must be provided", nameof(value));

            _globPatterns = value;
        }
    }

    private IEnumerable<string> _globPatterns = new[] { "*.xml" };

    [Option('l', "line-coverage-threshold", Default = 80, HelpText = "The line coverage percentage threshold.")]
    public int LineCoverageThreshold {
        get => _lineCoverageThreshold;
        set {
            if (value is < 0 or > 100)
                throw new ArgumentOutOfRangeException(nameof(value), "The line coverage percentage threshold must be between 0 and 100");

            _lineCoverageThreshold = value;
        }
    }

    private int _lineCoverageThreshold;

    [Option('b', "branch-coverage-threshold", Default = 80, HelpText = "The branch coverage percentage threshold.")]
    public int BranchCoverageThreshold {
        get => _branchCoverageThreshold;
        set {
            if (value is < 0 or > 100)
                throw new ArgumentOutOfRangeException(nameof(value), "The branch coverage percentage threshold must be between 0 and 100");

            _branchCoverageThreshold = value;
        }
    }

    private int _branchCoverageThreshold;

    [Option("fail-if-below-threshold", Default = true, HelpText = "Fail if the coverage does not meet the required threshold.")]
    public bool FailIfBelowThreshold { get; set; }

    [Option("fail-if-no-files-found", Default = true, HelpText = "Fail if no coverage files are found.")]
    public bool FailIfNoFilesFound { get; set; }
}