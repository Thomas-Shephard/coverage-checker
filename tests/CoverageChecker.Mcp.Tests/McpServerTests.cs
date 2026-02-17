using System.Text.Json;
using CoverageChecker.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CoverageChecker.Mcp.Tests;

public class McpServerTests
{
    private McpServer _sut;
    private Mock<IProcessExecutor> _mockExecutor;
    private Mock<IFileFinder> _mockFinder;
    private StringReader? _reader;
    private StringWriter? _writer;

    private static readonly string[] FullLineCoverageGlob = ["FullLineCoverage.xml"];
    private static readonly string[] AnyCoverageGlob = ["*Coverage.xml"];

    [SetUp]
    public void Setup()
    {
        _mockExecutor = new Mock<IProcessExecutor>();
        _mockFinder = new Mock<IFileFinder>();
        _sut = new McpServer(NullLoggerFactory.Instance, _mockExecutor.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
        _reader?.Dispose();
        _writer?.Dispose();
    }

    private void SetupCommunication(string input)
    {
        _reader = new StringReader(input);
        _writer = new StringWriter();
    }

    private async Task RunSutAsync(McpServer? sut = null)
    {
        McpServer target = sut ?? _sut;
        if (_reader == null || _writer == null) throw new InvalidOperationException("Communication not setup");
        await target.RunAsync(_reader, _writer);
    }

    private static string GetRepoRoot()
    {
        string? current = AppContext.BaseDirectory;
        while (current != null && !Directory.Exists(Path.Combine(current, ".git")))
        {
            current = Path.GetDirectoryName(current);
        }
        return current?.Replace('\\', '/') ?? throw new InvalidOperationException("Could not find repo root");
    }

    private McpServer CreateSutWithMockFinder()
    {
        return new McpServer(NullLoggerFactory.Instance, _mockExecutor.Object, _ => _mockFinder.Object);
    }

    [Test]
    public async Task RunAsyncShouldHandleInitialize()
    {
        var request = new { jsonrpc = "2.0", method = "initialize", id = 1 };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        await RunSutAsync();

        string responseJson = _writer?.ToString() ?? string.Empty;
        Assert.That(responseJson, Is.Not.Empty);
        JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        Assert.Multiple(() =>
        {
            Assert.That(response.GetProperty("id").GetInt32(), Is.EqualTo(1));
            Assert.That(response.GetProperty("result").GetProperty("serverInfo").GetProperty("name").GetString(), Is.EqualTo("coverage-checker"));
        });
    }

    [Test]
    public async Task RunAsyncShouldHandleToolsList()
    {
        var request = new { jsonrpc = "2.0", method = "tools/list", id = 2 };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        await RunSutAsync();

        string responseJson = _writer?.ToString() ?? string.Empty;
        JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        Assert.That(response.GetProperty("id").GetInt32(), Is.EqualTo(2));
        JsonElement tools = response.GetProperty("result").GetProperty("tools");
        Assert.That(tools.GetArrayLength(), Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public async Task RunAsyncShouldReturnErrorForUnknownMethod()
    {
        var request = new { jsonrpc = "2.0", method = "unknown", id = 3 };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        await RunSutAsync();

        string responseJson = _writer?.ToString() ?? string.Empty;
        JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        Assert.Multiple(() =>
        {
            Assert.That(response.TryGetProperty("error", out JsonElement error), Is.True);
            Assert.That(error.GetProperty("code").GetInt32(), Is.EqualTo(-32601));
        });
    }

    [Test]
    public async Task RunAsyncShouldHandleCallToolUnknownTool()
    {
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "non_existent_tool", arguments = new Dictionary<string, object>() },
            id = 4
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        await RunSutAsync();

        string responseJson = _writer?.ToString() ?? string.Empty;
        JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        JsonElement result = response.GetProperty("result");
        Assert.Multiple(() =>
        {
            Assert.That(result.GetProperty("isError").GetBoolean(), Is.True);
            Assert.That(result.GetProperty("content")[0].GetProperty("text").GetString(), Does.Contain("Unknown tool"));
        });
    }

    [Test]
    public async Task RunAsyncShouldHandleCallToolAnalyzeDeltaInvalidArgs()
    {
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "analyze_delta", arguments = new Dictionary<string, object>() },
            id = 5
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        await RunSutAsync();

        string responseJson = _writer?.ToString() ?? string.Empty;
        JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        JsonElement result = response.GetProperty("result");
        Assert.Multiple(() =>
        {
            Assert.That(result.GetProperty("isError").GetBoolean(), Is.True);
            Assert.That(result.GetProperty("content")[0].GetProperty("text").GetString(), Does.Contain("Error executing tool"));
        });
    }

    [Test]
    public async Task RunAsyncShouldExecuteRunTestsAndAnalyzeWithCleanup()
    {
        McpServer sut = CreateSutWithMockFinder();
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "run_tests_and_analyze",
                arguments = new Dictionary<string, object>
                {
                    { "testCommand", "dotnet test" },
                    { "format", "Cobertura" },
                    { "directory", "/repo" },
                    { "reportPath", "coverage.xml" },
                    { "cleanup", true }
                }
            },
            id = 6
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        _mockExecutor.Setup(e => e.ExecuteShellAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
                     .ReturnsAsync((0, "Test Output", ""));
        
        _mockFinder.Setup(f => f.FindFiles(It.IsAny<string>())).Returns(["/repo/coverage.xml"]);

        await RunSutAsync(sut);

        _mockExecutor.Verify(e => e.ExecuteShellAsync(It.Is<string>(c => c.Contains("dotnet test")), "/repo", It.IsAny<TimeSpan?>()), Times.Once);
        _mockFinder.Verify(f => f.FindFiles("/repo"), Times.AtLeastOnce);
    }

    [Test]
    public async Task RunAsyncShouldExecuteRunTestsAndAnalyzeWithPlaceholder()
    {
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "run_tests_and_analyze",
                arguments = new Dictionary<string, object>
                {
                    { "testCommand", "run --out {output}" },
                    { "format", "Cobertura" },
                    { "directory", "/repo" },
                    { "reportPath", "{output}/coverage.xml" }
                }
            },
            id = 200
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        _mockExecutor.Setup(e => e.ExecuteShellAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
                     .ReturnsAsync((0, "Test Output", ""));

        await RunSutAsync();

        _mockExecutor.Verify(e => e.ExecuteShellAsync(It.Is<string>(c => c.Contains("run --out") && c.Contains(".coverage-checker-mcp")), "/repo", It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Test]
    public async Task RunAsyncShouldHandleCallToolGetSummaryInvalidArgs()
    {
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "get_coverage_summary", arguments = new Dictionary<string, object>() },
            id = 7
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        await RunSutAsync();

        string responseJson = _writer?.ToString() ?? string.Empty;
        JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        JsonElement result = response.GetProperty("result");
        Assert.Multiple(() =>
        {
            Assert.That(result.GetProperty("isError").GetBoolean(), Is.True);
            Assert.That(result.GetProperty("content")[0].GetProperty("text").GetString(), Does.Contain("Error executing tool"));
        });
    }

    [Test]
    public async Task RunAsyncShouldExecuteAnalyzeDeltaWithValidArgs()
    {
        string coverageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverageFiles", "Cobertura");
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "analyze_delta",
                arguments = new Dictionary<string, object>
                {
                    { "format", "Cobertura" },
                    { "directory", coverageDir },
                    { "globPatterns", FullLineCoverageGlob },
                    { "baseBranch", "main" }
                }
            },
            id = 8
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        await RunSutAsync();

        string responseJson = _writer?.ToString() ?? string.Empty;
        JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        JsonElement result = response.GetProperty("result");
        Assert.That(result.TryGetProperty("content", out _), Is.True);
        string? text = result.GetProperty("content")[0].GetProperty("text").GetString();
        Assert.That(text, Does.Contain("Delta Coverage Results").Or.Contain("No changed lines found"));
    }

    [Test]
    public async Task RunAsyncShouldExecuteGetSummaryWithValidArgs()
    {
        string coverageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverageFiles", "Cobertura");
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "get_coverage_summary",
                arguments = new Dictionary<string, object>
                {
                    { "format", "Cobertura" },
                    { "directory", coverageDir },
                    { "globPatterns", FullLineCoverageGlob }
                }
            },
            id = 9
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        await RunSutAsync();

        string responseJson = _writer?.ToString() ?? string.Empty;
        JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        JsonElement result = response.GetProperty("result");
        Assert.That(result.TryGetProperty("content", out _), Is.True);
        string? text = result.GetProperty("content")[0].GetProperty("text").GetString();
        Assert.That(text, Does.Contain("Overall Coverage Summary"));
        Assert.That(text, Does.Contain("Total Files: 3"));
    }

    [Test]
    public async Task RunAsyncShouldExecuteGetSummaryWithDefaultGlobPatterns()
    {
        string coverageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverageFiles", "Cobertura");
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "get_coverage_summary",
                arguments = new Dictionary<string, object>
                {
                    { "format", "Cobertura" },
                    { "directory", coverageDir },
                    { "globPatterns", AnyCoverageGlob } // use a specific glob that doesn't include EmptyFile.xml
                }
            },
            id = 10
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        await RunSutAsync();

        string responseJson = _writer?.ToString() ?? string.Empty;
        JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        JsonElement result = response.GetProperty("result");
        Assert.That(result.TryGetProperty("content", out _), Is.True);
        string? text = result.GetProperty("content")[0].GetProperty("text").GetString();
        Assert.That(text, Does.Contain("Overall Coverage Summary"));
    }

    [Test]
    public async Task RunAsyncShouldExecuteGetSummaryWithNonArrayGlobPatterns()
    {
        string coverageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverageFiles", "Cobertura");
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "get_coverage_summary",
                arguments = new Dictionary<string, object>
                {
                    { "format", "Cobertura" },
                    { "directory", coverageDir },
                    { "globPatterns", "not-an-array" } // trigger else branch in ParseArgs
                }
            },
            id = 11
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        await RunSutAsync();

        string responseJson = _writer?.ToString() ?? string.Empty;
        JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        JsonElement result = response.GetProperty("result");
        Assert.That(result.TryGetProperty("content", out _), Is.True);
    }

    [Test]
    public async Task RunAsyncShouldExecuteAnalyzeDeltaWithRealGaps()
    {
        string repoRoot = GetRepoRoot();
        string tempDir = Path.Combine(repoRoot, "src", "CoverageChecker.Mcp");
        string coverageFile = Path.Combine(tempDir, $"temp_delta_{Path.GetRandomFileName()}.xml");
        
        string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                     "<coverage>\n" +
                     "    <sources>\n" +
                     "<source>" + repoRoot + "</source>\n    </sources>\n    <packages>\n        <package name=\"Mcp\">\n            <classes>\n                <class name=\"McpServer\" filename=\"src/CoverageChecker.Mcp/McpServer.cs\">\n                    <lines>\n                        <line number=\"226\" hits=\"0\"/>\n                    </lines>\n                </class>\n            </classes>\n        </package>\n    </packages>\n</coverage>";
        await File.WriteAllTextAsync(coverageFile, xml);

        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "analyze_delta",
                arguments = new Dictionary<string, object>
                {
                    { "format", "Cobertura" },
                    { "directory", tempDir },
                    { "globPatterns", new[] { Path.GetFileName(coverageFile) } },
                    { "baseBranch", "main" }
                }
            },
            id = 12
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        await RunSutAsync();

        string responseJson = _writer?.ToString() ?? string.Empty;
        JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        JsonElement result = response.GetProperty("result");
        string? text = result.GetProperty("content")[0].GetProperty("text").GetString();
        
        Assert.That(text, Does.Contain("Delta Coverage Results"));

        File.Delete(coverageFile);
    }

    [Test]
    public async Task RunAsyncShouldHandleCleanup()
    {
        McpServer sut = CreateSutWithMockFinder();
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "run_tests_and_analyze",
                arguments = new Dictionary<string, object>
                {
                    { "testCommand", "dotnet test" },
                    { "format", "Cobertura" },
                    { "directory", "." },
                    { "reportPath", "temp.xml" },
                    { "cleanup", true }
                }
            },
            id = 13
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        _mockExecutor.Setup(e => e.ExecuteShell(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
                     .Returns((0, "OK", ""));

        string tempFile = Path.GetTempFileName();
        _mockFinder.Setup(f => f.FindFiles(It.IsAny<string>())).Returns([tempFile]);

        await RunSutAsync(sut);

        Assert.That(File.Exists(tempFile), Is.False);
    }

    [Test]
    public async Task RunAsyncShouldHandleCleanupError()
    {
        _mockFinder.Setup(f => f.FindFiles(It.IsAny<string>())).Throws(new InvalidOperationException("Finder failed"));
        McpServer sut = CreateSutWithMockFinder();

        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "run_tests_and_analyze",
                arguments = new Dictionary<string, object>
                {
                    { "testCommand", "dotnet test" },
                    { "format", "Cobertura" },
                    { "directory", "." },
                    { "reportPath", "temp.xml" },
                    { "cleanup", true }
                }
            },
            id = 14
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        _mockExecutor.Setup(e => e.ExecuteShell(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
                     .Returns((0, "OK", ""));

        await RunSutAsync(sut);

        string responseJson = _writer?.ToString() ?? string.Empty;
        Assert.That(responseJson, Is.Not.Empty);
    }

    [Test]
    public async Task RunAsyncShouldHandleNotifications()
    {
        var request = new { jsonrpc = "2.0", method = "initialized" };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        await RunSutAsync();

        Assert.That(_writer?.ToString() ?? string.Empty, Is.Empty);
    }

    [Test]
    public async Task RunAsyncShouldWorkWithDefaultDependencies()
    {
        McpServer sut = new(NullLoggerFactory.Instance);
        var request = new { jsonrpc = "2.0", method = "tools/list", id = 100 };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        await RunSutAsync(sut);

        Assert.That(_writer?.ToString() ?? string.Empty, Is.Not.Empty);
    }

    [Test]
    public async Task RunAsyncShouldHandleCallToolAnalyzeDeltaNullArguments()
    {
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "analyze_delta", arguments = (object?)null },
            id = 101
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        await RunSutAsync();

        string responseJson = _writer?.ToString() ?? string.Empty;
        JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        JsonElement result = response.GetProperty("result");
        Assert.Multiple(() =>
        {
            Assert.That(result.GetProperty("isError").GetBoolean(), Is.True);
            Assert.That(result.GetProperty("content")[0].GetProperty("text").GetString(), Does.Contain("Arguments are required"));
        });
    }

    [Test]
    public async Task RunAsyncShouldExecuteAnalyzeDeltaWithOmittedBaseBranch()
    {
        string coverageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverageFiles", "Cobertura");
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "analyze_delta",
                arguments = new Dictionary<string, object>
                {
                    { "format", "Cobertura" },
                    { "directory", coverageDir },
                    { "globPatterns", FullLineCoverageGlob }
                }
            },
            id = 102
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        await RunSutAsync();

        string responseJson = _writer?.ToString() ?? string.Empty;
        JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        JsonElement result = response.GetProperty("result");
        string? text = result.GetProperty("content")[0].GetProperty("text").GetString();
        Assert.That(text, Does.Contain("Target: main").Or.Contain("No changed lines found"));
    }

    [Test]
    public async Task RunAsyncShouldExecuteGetSummaryWithNullGlobPatterns()
    {
        string coverageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverageFiles", "Cobertura");
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "get_coverage_summary",
                arguments = new Dictionary<string, object?>
                {
                    { "format", "Cobertura" },
                    { "directory", coverageDir },
                    { "globPatterns", null }
                }
            },
            id = 103
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        await RunSutAsync();

        string responseJson = _writer?.ToString() ?? string.Empty;
        JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        JsonElement result = response.GetProperty("result");
        Assert.That(result.TryGetProperty("content", out _), Is.True);
    }
}
