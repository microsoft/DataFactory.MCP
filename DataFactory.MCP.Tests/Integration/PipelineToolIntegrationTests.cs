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
    private const string InvalidJobInstanceId = "00000000-0000-0000-0000-000000000002";

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

    #region RunPipelineAsync - Unauthenticated

    [Fact]
    public async Task RunPipelineAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _pipelineTool.RunPipelineAsync(TestWorkspaceId, InvalidPipelineId);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task RunPipelineAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.RunPipelineAsync("", InvalidPipelineId);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task RunPipelineAsync_WithEmptyPipelineId_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.RunPipelineAsync(TestWorkspaceId, "");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("pipelineId"));
    }

    [Fact]
    public async Task RunPipelineAsync_WithInvalidExecutionDataJson_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.RunPipelineAsync(TestWorkspaceId, InvalidPipelineId, "not-valid-json{");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, "Invalid executionData JSON format");
    }

    [Fact]
    public async Task RunPipelineAsync_WithValidExecutionDataJson_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange
        var executionDataJson = "{\"param1\":\"value1\"}";

        // Act
        var result = await _pipelineTool.RunPipelineAsync(TestWorkspaceId, InvalidPipelineId, executionDataJson);

        // Assert
        AssertAuthenticationError(result);
    }

    #endregion

    #region GetPipelineRunStatusAsync - Unauthenticated

    [Fact]
    public async Task GetPipelineRunStatusAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _pipelineTool.GetPipelineRunStatusAsync(TestWorkspaceId, InvalidPipelineId, InvalidJobInstanceId);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task GetPipelineRunStatusAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.GetPipelineRunStatusAsync("", InvalidPipelineId, InvalidJobInstanceId);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task GetPipelineRunStatusAsync_WithEmptyPipelineId_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.GetPipelineRunStatusAsync(TestWorkspaceId, "", InvalidJobInstanceId);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("pipelineId"));
    }

    [Fact]
    public async Task GetPipelineRunStatusAsync_WithEmptyJobInstanceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.GetPipelineRunStatusAsync(TestWorkspaceId, InvalidPipelineId, "");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("jobInstanceId"));
    }

    #endregion

    #region CreatePipelineScheduleAsync - Unauthenticated

    [Fact]
    public async Task CreatePipelineScheduleAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange
        var configJson = "{\"type\":\"Cron\",\"startDateTime\":\"2024-04-28T00:00:00\",\"endDateTime\":\"2024-04-30T23:59:00\",\"localTimeZoneId\":\"Central Standard Time\",\"interval\":10}";

        // Act
        var result = await _pipelineTool.CreatePipelineScheduleAsync(TestWorkspaceId, InvalidPipelineId, true, configJson);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task CreatePipelineScheduleAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Arrange
        var configJson = "{\"type\":\"Cron\",\"interval\":10}";

        // Act
        var result = await _pipelineTool.CreatePipelineScheduleAsync("", InvalidPipelineId, true, configJson);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task CreatePipelineScheduleAsync_WithEmptyPipelineId_ShouldReturnValidationError()
    {
        // Arrange
        var configJson = "{\"type\":\"Cron\",\"interval\":10}";

        // Act
        var result = await _pipelineTool.CreatePipelineScheduleAsync(TestWorkspaceId, "", true, configJson);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("pipelineId"));
    }

    [Fact]
    public async Task CreatePipelineScheduleAsync_WithEmptyConfigurationJson_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.CreatePipelineScheduleAsync(TestWorkspaceId, InvalidPipelineId, true, "");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("configurationJson"));
    }

    [Fact]
    public async Task CreatePipelineScheduleAsync_WithInvalidConfigurationJson_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.CreatePipelineScheduleAsync(TestWorkspaceId, InvalidPipelineId, true, "not-valid-json{");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, "Invalid configuration JSON format");
    }

    #endregion

    #region ListPipelineSchedulesAsync - Unauthenticated

    [Fact]
    public async Task ListPipelineSchedulesAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _pipelineTool.ListPipelineSchedulesAsync(TestWorkspaceId, InvalidPipelineId);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListPipelineSchedulesAsync_WithContinuationToken_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange
        var testToken = "test-continuation-token";

        // Act
        var result = await _pipelineTool.ListPipelineSchedulesAsync(TestWorkspaceId, InvalidPipelineId, testToken);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListPipelineSchedulesAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.ListPipelineSchedulesAsync("", InvalidPipelineId);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task ListPipelineSchedulesAsync_WithEmptyPipelineId_ShouldReturnValidationError()
    {
        // Act
        var result = await _pipelineTool.ListPipelineSchedulesAsync(TestWorkspaceId, "");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("pipelineId"));
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

    [SkippableFact]
    public async Task RunPipelineAsync_WithAuthentication_NonExistentPipeline_ShouldReturnError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _pipelineTool.RunPipelineAsync(TestWorkspaceId, InvalidPipelineId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        SkipIfUpstreamBlocked(result);
        AssertNoAuthenticationError(result);
    }

    [SkippableFact]
    public async Task GetPipelineRunStatusAsync_WithAuthentication_NonExistentJobInstance_ShouldReturnError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _pipelineTool.GetPipelineRunStatusAsync(TestWorkspaceId, InvalidPipelineId, InvalidJobInstanceId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        SkipIfUpstreamBlocked(result);
        AssertNoAuthenticationError(result);
    }

    [SkippableFact]
    public async Task ListPipelineSchedulesAsync_WithAuthentication_ShouldReturnResultOrApiError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _pipelineTool.ListPipelineSchedulesAsync(TestWorkspaceId, InvalidPipelineId);

        // Assert
        AssertScheduleListResult(result);
    }

    [SkippableFact]
    public async Task ListPipelineSchedulesAsync_WithInvalidWorkspaceId_ShouldReturnApiError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _pipelineTool.ListPipelineSchedulesAsync(InvalidWorkspaceId, InvalidPipelineId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        AssertNoAuthenticationError(result);
        Assert.Contains("workspaceId must be a valid GUID", result);
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

    private static void AssertScheduleListResult(string result)
    {
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Skip if upstream service is blocking requests
        SkipIfUpstreamBlocked(result);

        AssertNoAuthenticationError(result);

        // Should either be "No schedules found" or valid JSON
        if (result.Contains("No schedules found"))
        {
            Assert.Contains("No schedules found", result);
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

            Assert.True(root.TryGetProperty("pipelineId", out _), "JSON response should have pipelineId property");
            Assert.True(root.TryGetProperty("workspaceId", out _), "JSON response should have workspaceId property");
            Assert.True(root.TryGetProperty("scheduleCount", out _), "JSON response should have scheduleCount property");
            Assert.True(root.TryGetProperty("schedules", out _), "JSON response should have schedules property");
        }
    }

    #endregion
}
