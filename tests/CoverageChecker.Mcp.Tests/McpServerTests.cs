using System.Text.Json;
using CoverageChecker.Mcp;
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

    [SetUp]
    public void Setup()
    {
        _mockExecutor = new Mock<IProcessExecutor>();
        _mockFinder = new Mock<IFileFinder>();
        _sut = new McpServer(NullLoggerFactory.Instance, _mockExecutor.Object, _ => _mockFinder.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _reader?.Dispose();
        _writer?.Dispose();
    }

    private void SetupCommunication(string input)
    {
        _reader = new StringReader(input);
        _writer = new StringWriter();
    }

    private string GetRepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !Directory.Exists(Path.Combine(current, ".git")))
        {
            current = Path.GetDirectoryName(current);
        }
        return current?.Replace('\\', '/') ?? throw new Exception("Could not find repo root");
    }

    [Test]
    public async Task RunAsync_ShouldHandleInitialize()
    {
        // Arrange
        var request = new { jsonrpc = "2.0", method = "initialize", id = 1 };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        var responseJson = _writer!.ToString();
        Assert.That(responseJson, Is.Not.Empty);
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        Assert.That(response.GetProperty("id").GetInt32(), Is.EqualTo(1));
        Assert.That(response.GetProperty("result").GetProperty("serverInfo").GetProperty("name").GetString(), Is.EqualTo("coverage-checker"));
    }

    [Test]
    public async Task RunAsync_ShouldHandleToolsList()
    {
        // Arrange
        var request = new { jsonrpc = "2.0", method = "tools/list", id = 2 };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        var responseJson = _writer!.ToString();
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        Assert.That(response.GetProperty("id").GetInt32(), Is.EqualTo(2));
        var tools = response.GetProperty("result").GetProperty("tools");
        Assert.That(tools.GetArrayLength(), Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public async Task RunAsync_ShouldReturnError_ForUnknownMethod()
    {
        // Arrange
        var request = new { jsonrpc = "2.0", method = "unknown", id = 3 };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        var responseJson = _writer!.ToString();
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        Assert.That(response.TryGetProperty("error", out var error), Is.True);
        Assert.That(error.GetProperty("code").GetInt32(), Is.EqualTo(-32601));
    }

    [Test]
    public async Task RunAsync_ShouldHandleCallTool_UnknownTool()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "non_existent_tool", arguments = new Dictionary<string, object>() },
            id = 4
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        var responseJson = _writer!.ToString();
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        var result = response.GetProperty("result");
        Assert.That(result.GetProperty("isError").GetBoolean(), Is.True);
        Assert.That(result.GetProperty("content")[0].GetProperty("text").GetString(), Does.Contain("Unknown tool"));
    }

    [Test]
    public async Task RunAsync_ShouldHandleCallTool_AnalyzeDelta_InvalidArgs()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "analyze_delta", arguments = new Dictionary<string, object>() },
            id = 5
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        var responseJson = _writer!.ToString();
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        var result = response.GetProperty("result");
        Assert.That(result.GetProperty("isError").GetBoolean(), Is.True);
        Assert.That(result.GetProperty("content")[0].GetProperty("text").GetString(), Does.Contain("Error executing tool"));
    }

    [Test]
    public async Task RunAsync_ShouldExecuteRunTestsAndAnalyze_WithCleanup()
    {
        // Arrange
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

        _mockExecutor.Setup(e => e.Execute(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
                     .Returns((0, "Test Output", ""));
        
        _mockFinder.Setup(f => f.FindFiles(It.IsAny<string>())).Returns(["/repo/coverage.xml"]);

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        _mockExecutor.Verify(e => e.Execute("dotnet", It.Is<IEnumerable<string>>(a => a.Contains("test")), "/repo", It.IsAny<TimeSpan?>()), Times.Once);
        _mockFinder.Verify(f => f.FindFiles("/repo"), Times.Once);
    }

    [Test]
    public async Task RunAsync_ShouldHandleCallTool_GetSummary_InvalidArgs()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "get_coverage_summary", arguments = new Dictionary<string, object>() },
            id = 7
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        var responseJson = _writer!.ToString();
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        var result = response.GetProperty("result");
        Assert.That(result.GetProperty("isError").GetBoolean(), Is.True);
        Assert.That(result.GetProperty("content")[0].GetProperty("text").GetString(), Does.Contain("Error executing tool"));
    }

    [Test]
    public async Task RunAsync_ShouldExecuteAnalyzeDelta_WithValidArgs()
    {
        // Arrange
        var coverageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverageFiles", "Cobertura");
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
                    { "globPatterns", new[] { "FullLineCoverage.xml" } },
                    { "baseBranch", "main" }
                }
            },
            id = 8
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        var responseJson = _writer!.ToString();
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        var result = response.GetProperty("result");
        Assert.That(result.TryGetProperty("content", out _), Is.True);
        var text = result.GetProperty("content")[0].GetProperty("text").GetString();
        Assert.That(text, Does.Contain("Delta Coverage Results").Or.Contain("No changed lines found"));
    }

    [Test]
    public async Task RunAsync_ShouldExecuteGetSummary_WithValidArgs()
    {
        // Arrange
        var coverageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverageFiles", "Cobertura");
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
                    { "globPatterns", new[] { "FullLineCoverage.xml" } }
                }
            },
            id = 9
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        var responseJson = _writer!.ToString();
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        var result = response.GetProperty("result");
        Assert.That(result.TryGetProperty("content", out _), Is.True);
        var text = result.GetProperty("content")[0].GetProperty("text").GetString();
        Assert.That(text, Does.Contain("Overall Coverage Summary"));
        Assert.That(text, Does.Contain("Total Files: 3"));
    }

    [Test]
    public async Task RunAsync_ShouldExecuteGetSummary_WithDefaultGlobPatterns()
    {
        // Arrange
        var coverageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverageFiles", "Cobertura");
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
                    { "globPatterns", new[] { "*Coverage.xml" } } // use a specific glob that doesn't include EmptyFile.xml
                }
            },
            id = 10
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        var responseJson = _writer!.ToString();
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        var result = response.GetProperty("result");
        Assert.That(result.TryGetProperty("content", out _), Is.True);
        var text = result.GetProperty("content")[0].GetProperty("text").GetString();
        Assert.That(text, Does.Contain("Overall Coverage Summary"));
    }

    [Test]
    public async Task RunAsync_ShouldExecuteGetSummary_WithNonArrayGlobPatterns()
    {
        // Arrange
        var coverageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverageFiles", "Cobertura");
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

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        var responseJson = _writer!.ToString();
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        var result = response.GetProperty("result");
        Assert.That(result.TryGetProperty("content", out _), Is.True);
    }

    [Test]
    public async Task RunAsync_ShouldExecuteAnalyzeDelta_WithRealGaps()
    {
        // Arrange
        var repoRoot = GetRepoRoot();
        var tempDir = Path.Combine(repoRoot, "src", "CoverageChecker.Mcp");
        var coverageFile = Path.Combine(tempDir, "temp_delta_coverage.xml");
        
        var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
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
                    { "globPatterns", new[] { "temp_delta_coverage.xml" } },
                    { "baseBranch", "main" }
                }
            },
            id = 12
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        var responseJson = _writer!.ToString();
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        var result = response.GetProperty("result");
        var text = result.GetProperty("content")[0].GetProperty("text").GetString();
        
        Assert.That(text, Does.Contain("Delta Coverage Results"));
        
        // Cleanup
        File.Delete(coverageFile);
    }

    [Test]
    public async Task RunAsync_ShouldHandleCleanup()
    {
        // Arrange
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

        _mockExecutor.Setup(e => e.Execute(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
                     .Returns((0, "OK", ""));
        
        // Mock finder to return a file that we can "delete"
        var tempFile = Path.GetTempFileName();
        _mockFinder.Setup(f => f.FindFiles(It.IsAny<string>())).Returns([tempFile]);

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        Assert.That(File.Exists(tempFile), Is.False);
    }

    [Test]
    public async Task RunAsync_ShouldHandleCleanupError()
    {
        // Arrange
        var sutWithFailingFinder = new McpServer(NullLoggerFactory.Instance, _mockExecutor.Object, _ => {
            var mock = new Mock<IFileFinder>();
            mock.Setup(f => f.FindFiles(It.IsAny<string>())).Throws(new Exception("Finder failed"));
            return mock.Object;
        });

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
        _reader = new StringReader(JsonSerializer.Serialize(request) + "\n");
        _writer = new StringWriter();

        _mockExecutor.Setup(e => e.Execute(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
                     .Returns((0, "OK", ""));

        // Act
        await sutWithFailingFinder.RunAsync(_reader!, _writer!);

        // Assert
        var responseJson = _writer!.ToString();
        Assert.That(responseJson, Is.Not.Empty);
    }

    [Test]
    public async Task RunAsync_ShouldHandleNotifications()
    {
        // Arrange
        var request = new { jsonrpc = "2.0", method = "initialized" };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        Assert.That(_writer!.ToString(), Is.Empty);
    }

    [Test]
    public async Task RunAsync_ShouldWorkWithDefaultDependencies()
    {
        // Arrange
        var sut = new McpServer(NullLoggerFactory.Instance);
        var request = new { jsonrpc = "2.0", method = "tools/list", id = 100 };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        // Act
        await sut.RunAsync(_reader!, _writer!);

        // Assert
        Assert.That(_writer!.ToString(), Is.Not.Empty);
    }

    [Test]
    public async Task RunAsync_ShouldHandleCallTool_AnalyzeDelta_NullArguments()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "analyze_delta", arguments = (object?)null },
            id = 101
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        var responseJson = _writer!.ToString();
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        var result = response.GetProperty("result");
        Assert.That(result.GetProperty("isError").GetBoolean(), Is.True);
        Assert.That(result.GetProperty("content")[0].GetProperty("text").GetString(), Does.Contain("Arguments are required"));
    }

    [Test]
    public async Task RunAsync_ShouldExecuteAnalyzeDelta_WithOmittedBaseBranch()
    {
        // Arrange
        var coverageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverageFiles", "Cobertura");
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
                    { "globPatterns", new[] { "FullLineCoverage.xml" } }
                }
            },
            id = 102
        };
        SetupCommunication(JsonSerializer.Serialize(request) + "\n");

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        var responseJson = _writer!.ToString();
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        var result = response.GetProperty("result");
        var text = result.GetProperty("content")[0].GetProperty("text").GetString();
        Assert.That(text, Does.Contain("Target: main").Or.Contain("No changed lines found"));
    }

    [Test]
    public async Task RunAsync_ShouldExecuteGetSummary_WithNullGlobPatterns()
    {
        // Arrange
        var coverageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverageFiles", "Cobertura");
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

        // Act
        await _sut.RunAsync(_reader!, _writer!);

        // Assert
        var responseJson = _writer!.ToString();
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        var result = response.GetProperty("result");
        Assert.That(result.TryGetProperty("content", out _), Is.True);
    }
}
