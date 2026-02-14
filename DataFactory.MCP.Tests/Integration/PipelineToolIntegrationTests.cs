using Xunit;
using DataFactory.MCP.Tools.Pipeline;
using DataFactory.MCP.Tests.Infrastructure;
using System.Text.Json;
using DataFactory.MCP.Models;

namespace DataFactory.MCP.Tests.Integration;

/// <summary>
/// Integration tests for PipelineTool that call the actual MCP tool methods
/// without mocking to verify real behavior
/// </summary>
public class PipelineToolIntegrationTests : FabricToolIntegrationTestBase
{
    private readonly PipelineTool _pipelineTool;

    // Test workspace IDs
    private const string TestWorkspaceId = "349f40ea-ecb0-4fe6-baf4-884b2887b074";
    private const string InvalidWorkspaceId = "invalid-workspace-id";
    private const string InvalidPipelineId = "00000000-0000-0000-0000-000000000001";

    public PipelineToolIntegrationTests(McpTestFixture fixture) : base(fixture)
    {
        _pipelineTool = Fixture.GetService<PipelineTool>();
    }

    #region DI Registration

    [Fact]
    public void PipelineTool_ShouldBeRegisteredInDI()
    {
        // Assert
        Assert.NotNull(_pipelineTool);
        Assert.IsType<PipelineTool>(_pipelineTool);
    }

    #endregion

    #region ListPipelinesAsync - Unauthenticated

    [Fact]
    public async Task ListPipelinesAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _pipelineTool.ListPipelinesAsync(TestWorkspaceId);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListPipelinesAsync_WithContinuationToken_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange
        var testToken = "test-continuation-token";

        // Act
        var result = await _pipelineTool.ListPipelinesAsync(TestWorkspaceId, testToken);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListPipelinesAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.ListPipelinesAsync("");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task ListPipelinesAsync_WithNullWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.ListPipelinesAsync(null!);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    #endregion

    #region GetPipelineAsync - Unauthenticated

    [Fact]
    public async Task GetPipelineAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _pipelineTool.GetPipelineAsync(TestWorkspaceId, InvalidPipelineId);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task GetPipelineAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.GetPipelineAsync("", InvalidPipelineId);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task GetPipelineAsync_WithEmptyPipelineId_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.GetPipelineAsync(TestWorkspaceId, "");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("pipelineId"));
    }

    #endregion

    #region CreatePipelineAsync - Unauthenticated

    [Fact]
    public async Task CreatePipelineAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _pipelineTool.CreatePipelineAsync(TestWorkspaceId, "test-pipeline");

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task CreatePipelineAsync_WithEmptyDisplayName_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.CreatePipelineAsync(TestWorkspaceId, "");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result);
    }

    #endregion

    #region UpdatePipelineAsync - Unauthenticated

    [Fact]
    public async Task UpdatePipelineAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _pipelineTool.UpdatePipelineAsync(TestWorkspaceId, InvalidPipelineId, displayName: "updated");

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task UpdatePipelineAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.UpdatePipelineAsync("", InvalidPipelineId, displayName: "updated");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task UpdatePipelineAsync_WithNoUpdates_ShouldReturnValidationError()
    {
        // Act - neither displayName nor description provided
        var result = await _pipelineTool.UpdatePipelineAsync(TestWorkspaceId, InvalidPipelineId);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, "At least one of displayName or description must be provided");
    }

    #endregion

    #region GetPipelineDefinitionAsync - Unauthenticated

    [Fact]
    public async Task GetPipelineDefinitionAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _pipelineTool.GetPipelineDefinitionAsync(TestWorkspaceId, InvalidPipelineId);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task GetPipelineDefinitionAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.GetPipelineDefinitionAsync("", InvalidPipelineId);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task GetPipelineDefinitionAsync_WithEmptyPipelineId_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.GetPipelineDefinitionAsync(TestWorkspaceId, "");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("pipelineId"));
    }

    #endregion

    #region UpdatePipelineDefinitionAsync - Unauthenticated

    [Fact]
    public async Task UpdatePipelineDefinitionAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange
        var definitionJson = "{\"activities\":[]}";

        // Act
        var result = await _pipelineTool.UpdatePipelineDefinitionAsync(TestWorkspaceId, InvalidPipelineId, definitionJson);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task UpdatePipelineDefinitionAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Arrange
        var definitionJson = "{\"activities\":[]}";

        // Act
        var result = await _pipelineTool.UpdatePipelineDefinitionAsync("", InvalidPipelineId, definitionJson);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task UpdatePipelineDefinitionAsync_WithInvalidJson_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.UpdatePipelineDefinitionAsync(TestWorkspaceId, InvalidPipelineId, "not-valid-json{");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, "Invalid JSON format");
    }

    [Fact]
    public async Task UpdatePipelineDefinitionAsync_WithEmptyDefinitionJson_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.UpdatePipelineDefinitionAsync(TestWorkspaceId, InvalidPipelineId, "");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("definitionJson"));
    }

    #endregion

    #region Authenticated Scenarios

    [SkippableFact]
    public async Task ListPipelinesAsync_WithAuthentication_ShouldReturnResultOrApiError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _pipelineTool.ListPipelinesAsync(TestWorkspaceId);

        // Assert
        AssertPipelineListResult(result);
    }

    [SkippableFact]
    public async Task ListPipelinesAsync_WithInvalidWorkspaceId_ShouldReturnApiError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _pipelineTool.ListPipelinesAsync(InvalidWorkspaceId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        AssertNoAuthenticationError(result);
        Assert.Contains("workspaceId must be a valid GUID", result);
    }

    [SkippableFact]
    public async Task GetPipelineAsync_WithAuthentication_NonExistentPipeline_ShouldReturnError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _pipelineTool.GetPipelineAsync(TestWorkspaceId, InvalidPipelineId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        SkipIfUpstreamBlocked(result);
        AssertNoAuthenticationError(result);
    }

    #endregion

    #region Helper Methods

    private static void AssertPipelineListResult(string result)
    {
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Skip if upstream service is blocking requests
        SkipIfUpstreamBlocked(result);

        AssertNoAuthenticationError(result);

        // Should either be "No pipelines found" or valid JSON
        if (result.Contains("No pipelines found"))
        {
            Assert.Contains("No pipelines found", result);
            return;
        }

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
            Assert.True(root.TryGetProperty("pipelineCount", out _), "JSON response should have pipelineCount property");
            Assert.True(root.TryGetProperty("pipelines", out _), "JSON response should have pipelines property");
        }
    }

    #endregion
}
