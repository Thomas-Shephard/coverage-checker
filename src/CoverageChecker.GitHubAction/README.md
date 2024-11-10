# Coverage Checker GitHub Action

The Coverage Checker GitHub action is a .NET based GitHub action for extracting code coverage metrics and ensuring that
they meet a specified threshold.

For general information about the Coverage Checker project, see the repository's
main [README file](https://github.com/Thomas-Shephard/coverage-checker/blob/main/README.md).

## Usage

The code coverage must already have been generated and saved in a file. This action will then parse the coverage file
and check the coverage. If desired, this action will fail if the coverage is below the specified threshold.

```yaml
- name: Check coverage
  id: check-coverage
  uses: Thomas-Shephard/coverage-checker@main
  with:
    format: 'Cobertura'
    directory: 'coverage'
    glob: '**/coverage.cobertura.xml'
    line-threshold: 80
    branch-threshold: 80
    fail-if-below-threshold: true
```

### Inputs

| Name                      | Description                                                        | Required | Default               |
|---------------------------|--------------------------------------------------------------------|----------|-----------------------|
| `format`                  | The format of the coverage file. Options: `SonarQube`, `Cobertura` | Yes      |                       |
| `directory`               | The directory containing the coverage file(s).                     | No       | The current directory |
| `glob-pattern`            | The glob pattern to match the coverage file(s).                    | Yes      |                       |
| `line-threshold`          | The line coverage threshold.                                       | No       | 80                    |
| `branch-threshold`        | The branch coverage threshold.                                     | No       | 80                    |
| `fail-if-below-threshold` | Whether to fail the action if the coverage is below the threshold. | No       | true                  |
| `fail-if-no-files-found`  | Whether to fail the action if no coverage files are found.         | No       | true                  |

### Outputs

| Name              | Description                         |
|-------------------|-------------------------------------|
| `line-coverage`   | The line coverage of the project.   |
| `branch-coverage` | The branch coverage of the project. |

These outputs can be used in subsequent steps, as shown below:

```yaml
- name: Use coverage results
  run: |
    echo "Line coverage ${{ steps.check-coverage.outputs.line-coverage }}"
    echo "Branch coverage ${{ steps.check-coverage.outputs.branch-coverage }}"
```