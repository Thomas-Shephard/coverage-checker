using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoverageChecker.Parsers;
using CoverageChecker.Results;
using CoverageChecker.Services;
using CoverageChecker.Utils;
using Microsoft.Extensions.Logging;

namespace CoverageChecker.Mcp;

internal sealed class McpServer : IDisposable
{
    private const string FormatLiteral = "format";
    private const string StringLiteral = "string";

    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IProcessExecutor _processExecutor;
    private readonly Func<IEnumerable<string>, IFileFinder> _fileFinderFactory;
    private readonly IGitService? _gitService;
    private readonly Func<CoverageAnalyserOptions, IFileFinder, CoverageAnalyser>? _analyserFactory;

    private readonly JsonSerializerOptions _jsonOptions = new() 
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly string[] CoverageFormats = Enum.GetNames<CoverageFormat>();
    private static readonly string[] FormatRequiredArgs = [FormatLiteral];
    private static readonly string[] RunTestsRequiredArgs = ["testCommand", FormatLiteral, "directory", "reportPath"];

    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly SemaphoreSlim _concurrencyLimiter = new(10, 10);
    private bool _disposed;

    public McpServer(
        ILoggerFactory loggerFactory,
        IProcessExecutor? processExecutor = null,
        Func<IEnumerable<string>, IFileFinder>? fileFinderFactory = null,
        IGitService? gitService = null,
        Func<CoverageAnalyserOptions, IFileFinder, CoverageAnalyser>? analyserFactory = null)
    {
        _logger = loggerFactory.CreateLogger<McpServer>();
        _loggerFactory = loggerFactory;
        _processExecutor = processExecutor ?? new ProcessExecutor();
        _fileFinderFactory = fileFinderFactory ?? (patterns => new FileFinder(patterns));
        _gitService = gitService;
        _analyserFactory = analyserFactory;
    }

    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _writeLock.Dispose();
            _concurrencyLimiter.Dispose();
        }

        _disposed = true;
    }

    public async Task RunAsync(TextReader reader, TextWriter writer)
    {
        List<Task> tasks = [];
        while (await reader.ReadLineAsync() is { } line)
        {
            _logger.LogReceivedLine(line);
            
            await _concurrencyLimiter.WaitAsync();
            tasks.RemoveAll(t => t.IsCompleted);
            
            tasks.Add(HandleRequestWithLimitAsync(line, writer));
        }
        await Task.WhenAll(tasks);
    }

    private async Task HandleRequestWithLimitAsync(string line, TextWriter writer)
    {
        try
        {
            await HandleRequestAsync(line, writer);
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }

    private async Task HandleRequestAsync(string line, TextWriter writer)
    {
        McpRequest? request = null;
        try
        {
            request = JsonSerializer.Deserialize<McpRequest>(line, _jsonOptions);
            if (request == null) return;

            McpResponse? response = await HandleRequest(request);

            // Only respond to requests with an ID (notifications have null ID and don't get a response)
            if (response != null && (request.Id != null || response.Error != null))
            {
                await SendResponse(response, writer);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogMcpRequestProcessingError(ex);
            await SendResponse(new McpResponse("2.0", null, Error: new McpError(-32700, $"Parse error: {ex.Message}")), writer);
        }
        catch (Exception ex)
        {
            _logger.LogMcpRequestProcessingError(ex);
            if (request?.Id != null)
            {
                await SendResponse(new McpResponse("2.0", request.Id, Error: new McpError(-32603, $"Internal error: {ex.Message}")), writer);
            }
        }
    }

    private async Task SendResponse(McpResponse response, TextWriter writer)
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
                        format = new { type = StringLiteral, @enum = CoverageFormats, description = "The coverage file format." },
                        directory = new { type = StringLiteral, description = "The directory to search for coverage files." },
                        globPatterns = new { type = "array", items = new { type = StringLiteral }, description = "Glob patterns to find coverage files (e.g. ['**/*.xml'])" },
                        baseBranch = new { type = StringLiteral, description = "The base branch/commit to compare against (e.g. 'main')." }
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
                        format = new { type = StringLiteral, @enum = CoverageFormats },
                        directory = new { type = StringLiteral },
                        globPatterns = new { type = "array", items = new { type = StringLiteral } }
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
                        testCommand = new { type = StringLiteral, description = "The command to run tests. Use '{output}' as a placeholder for the results directory (e.g., 'dotnet test --collect:\"XPlat Code Coverage\" --results-directory {output}')." },
                        format = new { type = StringLiteral, @enum = CoverageFormats },
                        directory = new { type = StringLiteral, description = "The root directory of the project." },
                        reportPath = new { type = StringLiteral, description = "The path or glob pattern to the coverage report generated by the test command. Often includes the '{output}' directory (e.g. '{output}/**/*.xml')." },
                        baseBranch = new { type = StringLiteral, description = "The base branch/commit for delta analysis. Default: 'main'." },
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
                "analyze_delta" => await ExecuteAnalyzeDelta(callRequest.Arguments),
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

    private CoverageAnalyser CreateAnalyser(CoverageAnalyserOptions options)
    {
        IFileFinder finder = _fileFinderFactory(options.GlobPatterns);
        if (_analyserFactory != null)
        {
            return _analyserFactory(options, finder);
        }

        if (_gitService != null)
        {
            return new CoverageAnalyser(
                options,
                finder,
                new ParserFactory(new CoverageMergeService(_loggerFactory.CreateLogger<CoverageMergeService>())),
                _gitService,
                new DeltaCoverageService(new CoverageMergeService(_loggerFactory.CreateLogger<CoverageMergeService>())),
                new CoverageRegressionService(),
                _loggerFactory);
        }

        return new CoverageAnalyser(options, finder, _loggerFactory);
    }

    private async Task<McpCallToolResponse> ExecuteRunTestsAndAnalyze(IDictionary<string, object?>? args)
    {
        RunTestsArgs runArgs = ExtractRunTestsArgs(args);
        
        // 1. Prepare Paths and Command
        string outputDir = Path.Combine(runArgs.Directory, ".coverage-checker-mcp", Guid.NewGuid().ToString());
        string testCommand = CommandUtils.PrepareCommand(runArgs.TestCommandTemplate, outputDir, _logger);
        string reportPath = PathUtils.MakeRelativeIfInside(runArgs.Directory, runArgs.ReportPathTemplate.Replace("{output}", outputDir));

        Directory.CreateDirectory(outputDir);

        try
        {
            // 2. Run Tests
            (int exitCode, string stdout, string stderr) = await _processExecutor.ExecuteShellAsync(testCommand, runArgs.Directory, runArgs.Timeout);

            StringBuilder outputBuilder = new();
            outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"Test Command Exited with code {exitCode}");
            AppendProcessOutput(outputBuilder, stdout, stderr);

            // 3. Analyze Coverage
            AnalyzeAndFormatDeltaResults(outputBuilder, runArgs.Format, runArgs.Directory, reportPath, runArgs.BaseBranch);

            return new McpCallToolResponse([new McpContent("text", outputBuilder.ToString())], exitCode != 0);
        }
        finally
        {
            // 4. Cleanup
            if (runArgs.Cleanup)
            {
                CleanupRunResources(runArgs.Directory, outputDir, reportPath);
            }
        }
    }

    private sealed record RunTestsArgs(
        string TestCommandTemplate,
        CoverageFormat Format,
        string Directory,
        string ReportPathTemplate,
        string BaseBranch,
        bool Cleanup,
        TimeSpan Timeout);

    private static RunTestsArgs ExtractRunTestsArgs(IDictionary<string, object?>? args)
    {
        if (args == null || 
            !args.TryGetValue("testCommand", out object? tc) ||
            !args.TryGetValue(FormatLiteral, out object? f) ||
            !args.TryGetValue("directory", out object? d) ||
            !args.TryGetValue("reportPath", out object? rp))
        {
            throw new ArgumentException("Missing required arguments: testCommand, format, directory, reportPath");
        }

        string testCommandTemplate = tc is JsonElement etc ? etc.GetString() ?? string.Empty : tc?.ToString() ?? string.Empty;
        string formatStr = f is JsonElement ef ? ef.GetString() ?? string.Empty : f?.ToString() ?? string.Empty;
        string directory = d is JsonElement ed ? ed.GetString() ?? string.Empty : d?.ToString() ?? string.Empty;
        string reportPathTemplate = rp is JsonElement erp ? erp.GetString() ?? string.Empty : rp?.ToString() ?? string.Empty;
        
        string baseBranch = GetBaseBranch(args);
        
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
        
        return new RunTestsArgs(testCommandTemplate, format, directory, reportPathTemplate, baseBranch, cleanup, timeout);
    }

    private static void AppendProcessOutput(StringBuilder sb, string stdout, string stderr)
    {
        if (!string.IsNullOrEmpty(stdout))
        {
            sb.AppendLine("Stdout:");
            sb.AppendLine(stdout);
        }
        if (!string.IsNullOrEmpty(stderr))
        {
            sb.AppendLine("Stderr:");
            sb.AppendLine(stderr);
        }
    }

    private void AnalyzeAndFormatDeltaResults(StringBuilder outputBuilder, CoverageFormat format, string directory, string reportPath, string baseBranch)
    {
        try
        {
            CoverageAnalyserOptions options = new()
            {
                CoverageFormat = format,
                Directory = directory,
                GlobPatterns = [reportPath]
            };
            CoverageAnalyser analyser = CreateAnalyser(options);
            DeltaResult delta = analyser.AnalyseDeltaCoverage(baseBranch);

            outputBuilder.AppendLine("\n--- Delta Coverage Analysis ---");
            if (!delta.HasChangedLines)
            {
                outputBuilder.AppendLine("No changed lines found in the current delta.");
            }
            else
            {
                FormatDeltaStats(outputBuilder, delta.Coverage, directory);
            }
        }
        catch (Exception ex)
        {
            outputBuilder.AppendLine(CultureInfo.InvariantCulture, $"\n--- Delta Coverage Analysis Failed ---\n{ex.Message}");
        }
    }

    private static void FormatDeltaStats(StringBuilder sb, Coverage coverage, string directory)
    {
        double line = coverage.CalculateOverallCoverage();
        double branch = coverage.CalculateOverallCoverage(CoverageType.Branch);
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Line Coverage: {line:P2}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"- Branch Coverage: {branch:P2}");
        sb.AppendLine("\nMissing Lines in Delta:");
        sb.Append(GetGaps(coverage, directory));
        sb.AppendLine();
    }

    private void CleanupRunResources(string directory, string outputDir, string reportPath)
    {
        if (Directory.Exists(outputDir))
        {
            try { Directory.Delete(outputDir, recursive: true); } catch { /* Ignore */ }
        }

        string fullReportPath = Path.IsPathRooted(reportPath) ? reportPath : Path.GetFullPath(Path.Combine(directory, reportPath));

        // If the report is inside the output directory that was just deleted, we're done.
        if (PathUtils.IsSubPathOf(outputDir, fullReportPath))
        {
            return;
        }

        // If the report is outside the temporary output directory,
        // only allow cleanup of specific files (no wildcards) to avoid accidental broad deletions.
        if (reportPath.Contains('*') || reportPath.Contains('?'))
        {
            return;
        }

        TryCleanupReports(directory, reportPath);
    }

    private Task<McpCallToolResponse> ExecuteAnalyzeDelta(IDictionary<string, object?>? args)
    {
        (CoverageFormat format, string directory, string[] globPatterns) = ParseArgs(args);
        string baseBranch = GetBaseBranch(args);

        CoverageAnalyserOptions options = new()
        {
            CoverageFormat = format,
            Directory = directory,
            GlobPatterns = globPatterns
        };
        CoverageAnalyser analyser = CreateAnalyser(options);
        DeltaResult delta = analyser.AnalyseDeltaCoverage(baseBranch);

        if (!delta.HasChangedLines)
        {
            return Task.FromResult(new McpCallToolResponse([new McpContent("text", "No changed lines found in the current delta.")]));
        }

        StringBuilder sb = new();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Delta Coverage Results (Target: {baseBranch}):");
        FormatDeltaStats(sb, delta.Coverage, directory);

        return Task.FromResult(new McpCallToolResponse([new McpContent("text", sb.ToString())]));
    }

    private static string GetBaseBranch(IDictionary<string, object?>? args)
    {
        if (args == null || !args.TryGetValue("baseBranch", out object? bb) || bb == null)
        {
            return "main";
        }

        return bb is JsonElement ebb ? ebb.GetString() ?? "main" : bb.ToString() ?? "main";
    }

    private static string GetDirectory(IDictionary<string, object?>? args)
    {
        if (args == null || !args.TryGetValue("directory", out object? dirObj) || dirObj == null)
        {
            return Environment.CurrentDirectory;
        }

        string? dir = dirObj is JsonElement edir ? edir.GetString() : dirObj.ToString();
        return string.IsNullOrWhiteSpace(dir) ? Environment.CurrentDirectory : dir;
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
        CoverageAnalyser analyser = CreateAnalyser(options);
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
        if (!args.TryGetValue(FormatLiteral, out object? f)) throw new ArgumentException("Missing required argument: format");

        string? formatStr = f is JsonElement ef ? ef.GetString() : f?.ToString();
        CoverageFormat format = Enum.Parse<CoverageFormat>(formatStr ?? "Auto", true);
        string directory = GetDirectory(args);
        
        string[] globPatterns = ParseGlobPatterns(args, directory);

        return (format, directory, globPatterns);
    }

    private static string[] ParseGlobPatterns(IDictionary<string, object?> args, string directory)
    {
        List<string> globPatterns = [];
        if (args.TryGetValue("globPatterns", out object? gp) && gp != null)
        {
            if (gp is JsonElement { ValueKind: JsonValueKind.Array } element)
            {
                foreach (JsonElement item in element.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String)
                        continue;

                    string pattern = item.GetString() ?? string.Empty;
                    globPatterns.Add(PathUtils.MakeRelativeIfInside(directory, pattern));
                }
            }
            else if (gp is JsonElement { ValueKind: JsonValueKind.String } s)
            {
                string pattern = s.GetString() ?? string.Empty;
                globPatterns.Add(PathUtils.MakeRelativeIfInside(directory, pattern));
            }
            else if (gp is string str)
            {
                globPatterns.Add(PathUtils.MakeRelativeIfInside(directory, str));
            }
        }

        if (globPatterns.Count == 0)
        {
            globPatterns.Add("*.xml");
        }

        return globPatterns.ToArray();
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
            FormatFileGaps(sb, file, rootDirectory);
        }
        return sb.Length == 0 ? "No gaps found!" : sb.ToString();
    }

    private static void FormatFileGaps(StringBuilder sb, FileCoverage file, string rootDirectory)
    {
        List<string> uncoveredLines = file.Lines
                                          .Where(l => !l.IsCovered)
                                          .Select(l => l.LineNumber.ToString(CultureInfo.InvariantCulture))
                                          .ToList();
        
        List<string> uncoveredBranches = file.Lines
                                             .Where(l => l is { IsCovered: true, Branches: > 0 } && l.CoveredBranches < l.Branches)
                                             .Select(l => $"{l.LineNumber.ToString(CultureInfo.InvariantCulture)} ({l.CoveredBranches?.ToString(CultureInfo.InvariantCulture) ?? "0"}/{l.Branches?.ToString(CultureInfo.InvariantCulture) ?? "0"} branches)")
                                             .ToList();

        if (uncoveredLines.Count <= 0 && uncoveredBranches.Count <= 0)
            return;

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
}
