using Xunit;
using DataFactory.MCP.Tools;
using DataFactory.MCP.Tools.Dataflow;
using DataFactory.MCP.Tests.Infrastructure;
using System.Text.Json;
using DataFactory.MCP.Models;

namespace DataFactory.MCP.Tests.Integration;

/// <summary>
/// Integration tests for DataflowTool that call the actual MCP tool methods
/// without mocking to verify real behavior
/// </summary>
public class DataflowToolIntegrationTests : FabricToolIntegrationTestBase
{
    private readonly DataflowTool _dataflowTool;

    // Test workspace IDs
    private const string TestWorkspaceId = "349f40ea-ecb0-4fe6-baf4-884b2887b074";
    private const string InvalidWorkspaceId = "invalid-workspace-id";

    public DataflowToolIntegrationTests(McpTestFixture fixture) : base(fixture)
    {
        _dataflowTool = Fixture.GetService<DataflowTool>();
    }

    [Fact]
    public async Task ListDataflowsAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _dataflowTool.ListDataflowsAsync(TestWorkspaceId);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListDataflowsAsync_WithContinuationToken_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange
        var testToken = "test-continuation-token";

        // Act
        var result = await _dataflowTool.ListDataflowsAsync(TestWorkspaceId, testToken);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListDataflowsAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _dataflowTool.ListDataflowsAsync("");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task ListDataflowsAsync_WithNullWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _dataflowTool.ListDataflowsAsync(null!);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public void DataflowTool_ShouldBeRegisteredInDI()
    {
        // Assert
        Assert.NotNull(_dataflowTool);
        Assert.IsType<DataflowTool>(_dataflowTool);
    }

    #region Authenticated Scenarios

    /// <summary>
    /// Test listing dataflows with authentication
    /// </summary>
    [SkippableFact]
    public async Task ListDataflowsAsync_WithAuthentication_ShouldReturnResultOrApiError()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _dataflowTool.ListDataflowsAsync(TestWorkspaceId);

        // Assert
        AssertDataflowResult(result);
    }

    [SkippableFact]
    public async Task ListDataflowsAsync_WithInvalidWorkspaceId_ShouldReturnApiError()
    {
        // Arrange - Try to authenticate  
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _dataflowTool.ListDataflowsAsync(InvalidWorkspaceId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        AssertNoAuthenticationError(result);

        // Should contain API error information
        Assert.Contains("workspaceId must be a valid GUID", result);
    }

    [SkippableFact]
    public async Task ListDataflowsAsync_WithContinuationToken_ShouldHandleTokenParameter()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        var testToken = "test-continuation-token-12345";

        // Act
        var result = await _dataflowTool.ListDataflowsAsync(TestWorkspaceId, testToken);

        // Assert
        AssertDataflowResult(result);
    }

    #endregion

    #region Helper Methods

    private static void AssertDataflowResult(string result)
    {
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Skip if upstream service is blocking requests
        SkipIfUpstreamBlocked(result);

        AssertNoAuthenticationError(result);

        // Should either be a valid response, "no dataflows found", or API error
        if (result.Contains("HttpRequestError"))
        {
            McpResponseAssertHelper.AssertHttpError(result);
            return;
        }

        // If it's JSON, verify basic structure
        if (IsValidJson(result))
        {
            var jsonDoc = JsonDocument.Parse(result);
            var root = jsonDoc.RootElement;

            Assert.True(root.TryGetProperty("workspaceId", out _), "JSON response should have workspaceId property");
            Assert.True(root.TryGetProperty("dataflowCount", out _), "JSON response should have dataflowCount property");
            Assert.True(root.TryGetProperty("dataflows", out _), "JSON response should have dataflows property");
        }
    }

    #endregion
}