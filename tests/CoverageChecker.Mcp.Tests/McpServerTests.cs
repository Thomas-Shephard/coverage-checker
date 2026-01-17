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
}