# Coverage Checker

[![Build, Test and Publish](https://github.com/Thomas-Shephard/coverage-checker/actions/workflows/build-test-and-publish.yml/badge.svg)](https://github.com/Thomas-Shephard/coverage-checker/actions/workflows/build-test-and-publish.yml)

Allows for the checking of code coverage using the provided .NET command line tool. It can also be used
as a development dependency if you need to extract code coverage metrics as part of a .NET project.

## Features

- **Multi-format support**: Works with Cobertura and SonarQube coverage formats.
- **Delta Coverage**: Analyze only the lines changed in your current branch compared to a base branch.
- **CI/CD Ready**: Automatic integration with GitHub Actions for workflow commands and job summaries.
- **Threshold enforcement**: Exit with non-zero codes if coverage targets aren't met.

## Projects

- [Coverage Checker Command Line Tool](https://github.com/Thomas-Shephard/coverage-checker/blob/main/src/CoverageChecker.CommandLine)
- [Coverage Checker Library](https://github.com/Thomas-Shephard/coverage-checker/blob/main/src/CoverageChecker)

## Contributions

Contributions are welcome! Read
the [contributing guide](https://github.com/Thomas-Shephard/coverage-checker/blob/main/CONTRIBUTING.md) to get started.

## License

This project is licensed under the MIT License. See
the [LICENSE](https://github.com/Thomas-Shephard/coverage-checker/blob/main/LICENSE) for details.