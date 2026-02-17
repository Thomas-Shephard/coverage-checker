using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoverageChecker.Results;
using CoverageChecker.Services;
using CoverageChecker.Utils;
using Microsoft.Extensions.Logging;

namespace CoverageChecker.Mcp;

internal class McpServer(
    ILoggerFactory loggerFactory,
    IProcessExecutor? processExecutor = null,
    Func<IEnumerable<string>, IFileFinder>? fileFinderFactory = null) : IDisposable
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<McpServer>();
    private readonly IProcessExecutor _processExecutor = processExecutor ?? new ProcessExecutor();
    private readonly Func<IEnumerable<string>, IFileFinder> _fileFinderFactory = fileFinderFactory ?? (patterns => new FileFinder(patterns));
    private readonly JsonSerializerOptions _jsonOptions = new() 
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly string[] CoverageFormats = ["SonarQube", "Cobertura"];
    private static readonly string[] FormatRequiredArgs = ["format"];
    private static readonly string[] RunTestsRequiredArgs = ["testCommand", "format", "directory", "reportPath"];

    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public void Dispose()
    {
        _writeLock.Dispose();
    }

    public async Task RunAsync(TextReader reader, TextWriter writer)
    {
        while (await reader.ReadLineAsync() is { } line)
        {
            _logger.LogReceivedLine(line);
            await HandleRequestAsync(line, writer);
        }
    }

    private async Task HandleRequestAsync(string line, TextWriter writer)
    {
        try
        {
            McpRequest? request = JsonSerializer.Deserialize<McpRequest>(line, _jsonOptions);
            if (request == null) return;

            McpResponse? response = await HandleRequest(request);

            // Only respond to requests with an ID (notifications have null ID and don't get a response)
            if (response != null && request.Id != null)
            {
                string responseJson = JsonSerializer.Serialize(response, _jsonOptions);
                await _writeLock.WaitAsync();
                try
                {
                    await writer.WriteLineAsync(responseJson);
                    await writer.FlushAsync();
                }
                finally
                {
                    _writeLock.Release();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogMcpRequestProcessingError(ex);
        }
    }

    private async Task<McpResponse?> HandleRequest(McpRequest request)
    {
        switch (request.Method)
        {
            case "initialize":
                return new McpResponse("2.0", request.Id, new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new { tools = new { listChanged = false } },
                    serverInfo = new { name = "coverage-checker", version = "1.0.0" }
                });
            case "notifications/initialized":
            case "initialized":
                _logger.LogClientInitialized();
                return null;
            case "tools/list":
                return new McpResponse("2.0", request.Id, ListTools());
            case "tools/call":
                return new McpResponse("2.0", request.Id, await CallTool(request.Params));
            default:
                return request.Id is not null
                    ? new McpResponse("2.0", request.Id, Error: new McpError(-32601, $"Method not found: {request.Method}"))
                    : null;
        }
    }

    private static McpToolListResponse ListTools()
    {
        return new McpToolListResponse(
        [
            new McpTool(
                "analyze_delta",
                "Analyzes coverage for changed lines (delta) compared to a base branch or commit.",
                new {
                    type = "object",
                    properties = new {
                        format = new { type = "string", @enum = CoverageFormats, description = "The coverage file format." },
                        directory = new { type = "string", description = "The directory to search for coverage files." },
                        globPatterns = new { type = "array", items = new { type = "string" }, description = "Glob patterns to find coverage files (e.g. ['**/*.xml'])" },
                        baseBranch = new { type = "string", description = "The base branch/commit to compare against (e.g. 'main')." }
                    },
                    required = FormatRequiredArgs
                }
            ),
            new McpTool(
                "get_coverage_summary",
                "Provides a high-level summary of the overall code coverage.",
                new {
                    type = "object",
                    properties = new {
                        format = new { type = "string", @enum = CoverageFormats },
                        directory = new { type = "string" },
                        globPatterns = new { type = "array", items = new { type = "string" } }
                    },
                    required = FormatRequiredArgs
                }
            ),
            new McpTool(
                "run_tests_and_analyze",
                "Runs a test command and then performs delta coverage analysis on the resulting report.",
                new {
                    type = "object",
                    properties = new {
                        testCommand = new { type = "string", description = "The command to run tests. Use '{output}' as a placeholder for the results directory (e.g., 'dotnet test --collect:\"XPlat Code Coverage\" --results-directory {output}')." },
                        format = new { type = "string", @enum = CoverageFormats },
                        directory = new { type = "string", description = "The root directory of the project." },
                        reportPath = new { type = "string", description = "The path or glob pattern to the coverage report generated by the test command. Often includes the '{output}' directory (e.g. '{output}/**/*.xml')." },
                        baseBranch = new { type = "string", description = "The base branch/commit for delta analysis. Default: 'main'." },
                        cleanup = new { type = "boolean", description = "Whether to delete the coverage reports after analysis. Default: true." },
                        timeoutMinutes = new { type = "number", description = "Timeout for the test command in minutes. Default: 5." }
                    },
                    required = RunTestsRequiredArgs
                }
            )
        ]);
    }

    private async Task<object> CallTool(object? paramsObj)
    {
        if (paramsObj == null) return new McpCallToolResponse([new McpContent("text", "Missing parameters")], true);
        
        McpCallToolRequest? callRequest = paramsObj is JsonElement element
            ? element.Deserialize<McpCallToolRequest>(_jsonOptions)
            : JsonSerializer.Deserialize<McpCallToolRequest>(paramsObj.ToString() ?? string.Empty, _jsonOptions);

        if (callRequest == null) return new McpCallToolResponse([new McpContent("text", "Invalid tool call request")], true);

        try
        {
            return callRequest.Name switch
            {
                "analyze_delta" => ExecuteAnalyzeDelta(callRequest.Arguments),
                "get_coverage_summary" => ExecuteGetSummary(callRequest.Arguments),
                "run_tests_and_analyze" => await ExecuteRunTestsAndAnalyze(callRequest.Arguments),
                _ => new McpCallToolResponse([new McpContent("text", $"Unknown tool: {callRequest.Name}")], true)
            };
        }
        catch (Exception ex)
        {
            return new McpCallToolResponse([new McpContent("text", $"Error executing tool: {ex.Message}")], true);
        }
    }

    private Task<McpCallToolResponse> ExecuteRunTestsAndAnalyze(IDictionary<string, object?>? args)
    {
        if (args == null || 
            !args.TryGetValue("testCommand", out object? tc) ||
            !args.TryGetValue("format", out object? f) ||
            !args.TryGetValue("directory", out object? d) ||
            !args.TryGetValue("reportPath", out object? rp))
        {
            throw new ArgumentException("Missing required arguments: testCommand, format, directory, reportPath");
        }

        string testCommandTemplate = tc is JsonElement etc ? etc.GetString() ?? string.Empty : tc?.ToString() ?? string.Empty;
        string formatStr = f is JsonElement ef ? ef.GetString() ?? string.Empty : f?.ToString() ?? string.Empty;
        string directory = d is JsonElement ed ? ed.GetString() ?? string.Empty : d?.ToString() ?? string.Empty;
        string reportPathTemplate = rp is JsonElement erp ? erp.GetString() ?? string.Empty : rp?.ToString() ?? string.Empty;
        string baseBranch = args.TryGetValue("baseBranch", out object? bb) ? (bb is JsonElement ebb ? ebb.GetString() ?? "main" : bb?.ToString() ?? "main") : "main";
        
        bool cleanup = true;
        if (args.TryGetValue("cleanup", out object? c))
        {
            cleanup = c switch
            {
                JsonElement ce => ce.ValueKind != JsonValueKind.False,
                bool b         => b,
                _              => cleanup
            };
        }
        
        TimeSpan timeout = args.TryGetValue("timeoutMinutes", out object? t) && t is JsonElement te && te.TryGetDouble(out double mins)
            ? TimeSpan.FromMinutes(mins) 
            : TimeSpan.FromMinutes(5);

        CoverageFormat format = Enum.Parse<CoverageFormat>(formatStr, true);

        // 1. Prepare Paths and Command
        string outputDir = Path.Combine(directory, ".coverage-checker-mcp", Guid.NewGuid().ToString());
        string testCommand = CommandUtils.PrepareCommand(testCommandTemplate, outputDir, _logger);
        string reportPath = reportPathTemplate.Replace("{output}", outputDir);
        if (Path.IsPathRooted(reportPath) && reportPath.StartsWith(directory, StringComparison.OrdinalIgnoreCase))
        {
            reportPath = Path.GetRelativePath(directory, reportPath);
        }

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        try
        {
            // 2. Run Tests
            (int exitCode, string stdout, string stderr) = _processExecutor.ExecuteShell(testCommand, directory, timeout);

            StringBuilder outputBuilder = new();
            outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"Test Command Exited with code {exitCode}");
            if (!string.IsNullOrEmpty(stdout))
            {
                outputBuilder.AppendLine("Stdout:");
                outputBuilder.AppendLine(stdout);
            }
            if (!string.IsNullOrEmpty(stderr))
            {
                outputBuilder.AppendLine("Stderr:");
                outputBuilder.AppendLine(stderr);
            }

            try
            {
                // 3. Analyze Coverage
                CoverageAnalyserOptions options = new()
                {
                    CoverageFormat = format,
                    Directory = directory,
                    GlobPatterns = [reportPath]
                };
                CoverageAnalyser analyser = new(options, _fileFinderFactory(options.GlobPatterns), loggerFactory);
                DeltaResult delta = analyser.AnalyseDeltaCoverage(baseBranch);

                outputBuilder.AppendLine("\n--- Delta Coverage Analysis ---");
                if (!delta.HasChangedLines)
                {
                    outputBuilder.AppendLine("No changed lines found in the current delta.");
                }
                else
                {
                    double line = delta.Coverage.CalculateOverallCoverage();
                    double branch = delta.Coverage.CalculateOverallCoverage(CoverageType.Branch);
                    outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"- Line Coverage: {line:P2}");
                    outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"- Branch Coverage: {branch:P2}");
                    outputBuilder.AppendLine("\nMissing Lines in Delta:");
                    outputBuilder.Append(GetGaps(delta.Coverage, directory));
                    outputBuilder.AppendLine();
                }
            }
            catch (Exception ex)
            {
                outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"\n--- Delta Coverage Analysis Failed ---\n{ex.Message}");
            }

            return Task.FromResult(new McpCallToolResponse([new McpContent("text", outputBuilder.ToString())], exitCode != 0));
        }
        finally
        {
            // 4. Cleanup
            if (cleanup)
            {
                TryCleanupReports(directory, reportPath);
                if (Directory.Exists(outputDir))
                {
                    try { Directory.Delete(outputDir, recursive: true); } catch { /* Ignore */ }
                }
            }
        }
    }

    private McpCallToolResponse ExecuteAnalyzeDelta(IDictionary<string, object?>? args)
    {
        (CoverageFormat format, string directory, string[] globPatterns) = ParseArgs(args);
        string baseBranch = args?.TryGetValue("baseBranch", out object? bb) == true ? bb?.ToString() ?? "main" : "main";

        CoverageAnalyserOptions options = new()
        {
            CoverageFormat = format,
            Directory = directory,
            GlobPatterns = globPatterns
        };
        CoverageAnalyser analyser = new(options, _fileFinderFactory(options.GlobPatterns), loggerFactory);
        DeltaResult delta = analyser.AnalyseDeltaCoverage(baseBranch);

        if (!delta.HasChangedLines)
        {
            return new McpCallToolResponse([new McpContent("text", "No changed lines found in the current delta.")]);
        }

        double line = delta.Coverage.CalculateOverallCoverage();
        double branch = delta.Coverage.CalculateOverallCoverage(CoverageType.Branch);

        string report = $"Delta Coverage Results (Target: {baseBranch}):\n" +
                        $"- Line Coverage: {line:P2}\n" +
                        $"- Branch Coverage: {branch:P2}\n\n" +
                        "Missing Lines in Delta:\n" + GetGaps(delta.Coverage, directory);

        return new McpCallToolResponse([new McpContent("text", report)]);
    }

    private McpCallToolResponse ExecuteGetSummary(IDictionary<string, object?>? args)
    {
        (CoverageFormat format, string directory, string[] globPatterns) = ParseArgs(args);
        CoverageAnalyserOptions options = new()
        {
            CoverageFormat = format,
            Directory = directory,
            GlobPatterns = globPatterns
        };
        CoverageAnalyser analyser = new(options, _fileFinderFactory(options.GlobPatterns), loggerFactory);
        Coverage coverage = analyser.AnalyseCoverage();

        double line = coverage.CalculateOverallCoverage();
        double branch = coverage.CalculateOverallCoverage(CoverageType.Branch);

        string report = $"Overall Coverage Summary:\n" +
                        $"- Line Coverage: {line:P2}\n" +
                        $"- Branch Coverage: {branch:P2}\n" +
                        $"- Total Files: {coverage.Files.Count}";

        return new McpCallToolResponse([new McpContent("text", report)]);
    }

    private static (CoverageFormat Format, string Directory, string[] GlobPatterns) ParseArgs(IDictionary<string, object?>? args)
    {
        if (args == null) throw new ArgumentException("Arguments are required");
        if (!args.TryGetValue("format", out object? f)) throw new ArgumentException("Missing required argument: format");

        string? formatStr = f is JsonElement ef ? ef.GetString() : f?.ToString();
        CoverageFormat format = Enum.Parse<CoverageFormat>(formatStr ?? "Auto", true);
        string directory = args.TryGetValue("directory", out object? dirObj) && (dirObj is JsonElement edir ? edir.GetString() : dirObj?.ToString()) is { Length: > 0 } dirStr
            ? dirStr 
            : Environment.CurrentDirectory;
        
        List<string> globPatterns = [];
        if (args.TryGetValue("globPatterns", out object? gp))
        {
            if (gp is JsonElement { ValueKind: JsonValueKind.Array } element)
            {
                foreach (JsonElement item in element.EnumerateArray())
                {
                    string pattern = item.GetString() ?? string.Empty;
                    if (Path.IsPathRooted(pattern) && pattern.StartsWith(directory, StringComparison.OrdinalIgnoreCase))
                    {
                        pattern = Path.GetRelativePath(directory, pattern);
                    }
                    globPatterns.Add(pattern);
                }
            }
            else if (gp is JsonElement { ValueKind: JsonValueKind.String } s)
            {
                string pattern = s.GetString() ?? string.Empty;
                if (Path.IsPathRooted(pattern) && pattern.StartsWith(directory, StringComparison.OrdinalIgnoreCase))
                {
                    pattern = Path.GetRelativePath(directory, pattern);
                }
                globPatterns.Add(pattern);
            }
        }

        if (globPatterns.Count == 0)
        {
            globPatterns.Add("*.xml");
        }

        return (format, directory, globPatterns.ToArray());
    }

    private void TryCleanupReports(string directory, string reportPath)
    {
        try
        {
            // Always search from the root directory as reportPath is made relative to it
            IFileFinder finder = _fileFinderFactory([reportPath]);
            IEnumerable<string> filesToDelete = finder.FindFiles(directory).Where(File.Exists);
            foreach (string file in filesToDelete)
            {
                File.Delete(file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCleanupError(ex, reportPath);
        }
    }

    private static string GetGaps(Coverage coverage, string rootDirectory)
    {
        StringBuilder sb = new();
        foreach (FileCoverage file in coverage.Files)
        {
            List<string> uncoveredLines = file.Lines.Where(l => !l.IsCovered).Select(l => l.LineNumber.ToString(CultureInfo.InvariantCulture)).ToList();
            List<string> uncoveredBranches = file.Lines.Where(l => l is { IsCovered: true, Branches: > 0 } && l.CoveredBranches < l.Branches)
                                                 .Select(l => $"{l.LineNumber.ToString(CultureInfo.InvariantCulture)} ({l.CoveredBranches?.ToString(CultureInfo.InvariantCulture) ?? "0"}/{l.Branches?.ToString(CultureInfo.InvariantCulture) ?? "0"} branches)")
                                                 .ToList();

            if (uncoveredLines.Count <= 0 && uncoveredBranches.Count <= 0)
                continue;

            string displayPath = Path.IsPathRooted(file.Path) ? Path.GetRelativePath(rootDirectory, file.Path) : file.Path;
            sb.Append(CultureInfo.InvariantCulture, $"- {displayPath}:\n");
            if (uncoveredLines.Count > 0)
            {
                sb.AppendLine("  - Uncovered Lines:");
                foreach (string[] chunk in uncoveredLines.Chunk(10))
                {
                    sb.Append("    ").AppendLine(string.Join(", ", chunk));
                }
            }

            if (uncoveredBranches.Count > 0)
            {
                sb.AppendLine("  - Partial Branches:");
                foreach (string[] chunk in uncoveredBranches.Chunk(5))
                {
                    sb.Append("    ").AppendLine(string.Join(", ", chunk));
                }
            }
        }
        return sb.Length == 0 ? "No gaps found!" : sb.ToString();
    }
}
