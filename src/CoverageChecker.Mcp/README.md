# Coverage Checker MCP Server

This is a [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server for the Coverage Checker tool. It allows AI coding agents (like Claude Desktop, Cursor, or Windsurf) to directly query your project's code coverage data and identify gaps in real-time.

## Installation

Install the MCP server as a global .NET tool:

```bash
dotnet tool install --global CoverageChecker.Mcp
```

## AI Configuration

To use this with an AI agent, you need to add it to your agent's configuration file.

### Claude Desktop
Add this to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "coverage": {
      "command": "coveragechecker-mcp"
    }
  }
}
```

## Tools Provided

### `get_coverage_summary`
Returns a high-level overview of the project's coverage.
- **Inputs**: `format` (SonarQube/Cobertura), `directory`, `globPatterns` (array).

### `analyze_delta`
Performs a delta analysis to find uncovered lines in code that has changed compared to a base branch.
- **Inputs**: `format`, `directory`, `globPatterns`, `baseBranch` (e.g., "main").
- **Output**: A report showing Line/Branch coverage for the delta and a specific list of missing line numbers per file.

### `run_tests_and_analyze`
Orchestrates a full test run followed by delta coverage analysis. Ideal for AI agents verifying new tests.
- **Inputs**: 
  - `testCommand` (e.g., `dotnet test`)
  - `format` (SonarQube/Cobertura)
  - `directory` (Root dir)
  - `reportPath` (Glob to the report)
  - `baseBranch` (Optional)
  - `cleanup` (Boolean, Optional): If `true`, deletes the report files after analysis to keep the workspace clean.
- **Output**: The console output of the test run followed by the Delta Coverage report.

## Tips for AI Agents

- **Absolute Paths**: When using `directory`, it is highly recommended to provide the **absolute path** to the repository root. AI agents often operate in a virtualized or relative context, and absolute paths ensure the underlying Git commands resolve correctly.
- **Iterative Testing**: Use `run_tests_and_analyze` with `cleanup: true` to verify your new tests without leaving XML artifacts in the developer's workspace.
- **Delta Focus**: Always prefer `analyze_delta` over `get_coverage_summary` when fixing specific bugs or implementing features. It helps keep your context window clean by only showing the lines you actually touched.

## Requirements

- **Git**: Git must be installed and available on the system `PATH`.
- **Coverage Reports**: You must have a test runner (like `coverlet`) configured to produce Cobertura or SonarQube XML reports.

## Why use this?
Traditional coverage reports are often too large for AI context windows. This MCP server acts as a filter, providing the AI with only the **actionable gaps** it needs to fix, making it significantly more effective at writing tests.
