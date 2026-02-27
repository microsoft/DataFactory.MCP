using Xunit;
using DataFactory.MCP.Tools.CopyJob;
using DataFactory.MCP.Tests.Infrastructure;
using System.Text.Json;
using DataFactory.MCP.Models;

namespace DataFactory.MCP.Tests.Integration;

/// <summary>
/// Integration tests for CopyJobTool that call the actual MCP tool methods
/// without mocking to verify real behavior
/// </summary>
public class CopyJobToolIntegrationTests : FabricToolIntegrationTestBase
{
    private readonly CopyJobTool _copyJobTool;

    // Test workspace IDs
    private const string TestWorkspaceId = "349f40ea-ecb0-4fe6-baf4-884b2887b074";
    private const string InvalidWorkspaceId = "invalid-workspace-id";
    private const string InvalidCopyJobId = "00000000-0000-0000-0000-000000000001";
    private const string InvalidJobInstanceId = "00000000-0000-0000-0000-000000000002";

    public CopyJobToolIntegrationTests(McpTestFixture fixture) : base(fixture)
    {
        _copyJobTool = Fixture.GetService<CopyJobTool>();
    }

    #region DI Registration

    [Fact]
    public void CopyJobTool_ShouldBeRegisteredInDI()
    {
        // Assert
        Assert.NotNull(_copyJobTool);
        Assert.IsType<CopyJobTool>(_copyJobTool);
    }

    #endregion

    #region ListCopyJobsAsync - Unauthenticated

    [Fact]
    public async Task ListCopyJobsAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _copyJobTool.ListCopyJobsAsync(TestWorkspaceId);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListCopyJobsAsync_WithContinuationToken_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange
        var testToken = "test-continuation-token";

        // Act
        var result = await _copyJobTool.ListCopyJobsAsync(TestWorkspaceId, testToken);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListCopyJobsAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.ListCopyJobsAsync("");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task ListCopyJobsAsync_WithNullWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.ListCopyJobsAsync(null!);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    #endregion

    #region GetCopyJobAsync - Unauthenticated

    [Fact]
    public async Task GetCopyJobAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _copyJobTool.GetCopyJobAsync(TestWorkspaceId, InvalidCopyJobId);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task GetCopyJobAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.GetCopyJobAsync("", InvalidCopyJobId);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task GetCopyJobAsync_WithEmptyCopyJobId_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.GetCopyJobAsync(TestWorkspaceId, "");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("copyJobId"));
    }

    #endregion

    #region CreateCopyJobAsync - Unauthenticated

    [Fact]
    public async Task CreateCopyJobAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _copyJobTool.CreateCopyJobAsync(TestWorkspaceId, "test-copy-job");

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task CreateCopyJobAsync_WithEmptyDisplayName_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.CreateCopyJobAsync(TestWorkspaceId, "");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result);
    }

    #endregion

    #region UpdateCopyJobAsync - Unauthenticated

    [Fact]
    public async Task UpdateCopyJobAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _copyJobTool.UpdateCopyJobAsync(TestWorkspaceId, InvalidCopyJobId, displayName: "updated");

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task UpdateCopyJobAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.UpdateCopyJobAsync("", InvalidCopyJobId, displayName: "updated");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task UpdateCopyJobAsync_WithNoUpdates_ShouldReturnValidationError()
    {
        // Act - neither displayName nor description provided
        var result = await _copyJobTool.UpdateCopyJobAsync(TestWorkspaceId, InvalidCopyJobId);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, "At least one of displayName or description must be provided");
    }

    #endregion

    #region GetCopyJobDefinitionAsync - Unauthenticated

    [Fact]
    public async Task GetCopyJobDefinitionAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _copyJobTool.GetCopyJobDefinitionAsync(TestWorkspaceId, InvalidCopyJobId);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task GetCopyJobDefinitionAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.GetCopyJobDefinitionAsync("", InvalidCopyJobId);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task GetCopyJobDefinitionAsync_WithEmptyCopyJobId_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.GetCopyJobDefinitionAsync(TestWorkspaceId, "");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("copyJobId"));
    }

    #endregion

    #region UpdateCopyJobDefinitionAsync - Unauthenticated

    [Fact]
    public async Task UpdateCopyJobDefinitionAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange
        var definitionJson = "{\"source\":{},\"destination\":{}}";

        // Act
        var result = await _copyJobTool.UpdateCopyJobDefinitionAsync(TestWorkspaceId, InvalidCopyJobId, definitionJson);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task UpdateCopyJobDefinitionAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Arrange
        var definitionJson = "{\"source\":{},\"destination\":{}}";

        // Act
        var result = await _copyJobTool.UpdateCopyJobDefinitionAsync("", InvalidCopyJobId, definitionJson);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task UpdateCopyJobDefinitionAsync_WithInvalidJson_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.UpdateCopyJobDefinitionAsync(TestWorkspaceId, InvalidCopyJobId, "not-valid-json{");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, "Invalid JSON format");
    }

    [Fact]
    public async Task UpdateCopyJobDefinitionAsync_WithEmptyDefinitionJson_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.UpdateCopyJobDefinitionAsync(TestWorkspaceId, InvalidCopyJobId, "");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("definitionJson"));
    }

    #endregion

    #region RunCopyJobAsync - Unauthenticated

    [Fact]
    public async Task RunCopyJobAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _copyJobTool.RunCopyJobAsync(TestWorkspaceId, InvalidCopyJobId);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task RunCopyJobAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.RunCopyJobAsync("", InvalidCopyJobId);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task RunCopyJobAsync_WithEmptyCopyJobId_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.RunCopyJobAsync(TestWorkspaceId, "");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("copyJobId"));
    }

    [Fact]
    public async Task RunCopyJobAsync_WithInvalidExecutionDataJson_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.RunCopyJobAsync(TestWorkspaceId, InvalidCopyJobId, "not-valid-json{");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, "Invalid executionData JSON format");
    }

    [Fact]
    public async Task RunCopyJobAsync_WithValidExecutionDataJson_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange
        var executionDataJson = "{\"param1\":\"value1\"}";

        // Act
        var result = await _copyJobTool.RunCopyJobAsync(TestWorkspaceId, InvalidCopyJobId, executionDataJson);

        // Assert
        AssertAuthenticationError(result);
    }

    #endregion

    #region GetCopyJobRunStatusAsync - Unauthenticated

    [Fact]
    public async Task GetCopyJobRunStatusAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _copyJobTool.GetCopyJobRunStatusAsync(TestWorkspaceId, InvalidCopyJobId, InvalidJobInstanceId);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task GetCopyJobRunStatusAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.GetCopyJobRunStatusAsync("", InvalidCopyJobId, InvalidJobInstanceId);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task GetCopyJobRunStatusAsync_WithEmptyCopyJobId_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.GetCopyJobRunStatusAsync(TestWorkspaceId, "", InvalidJobInstanceId);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("copyJobId"));
    }

    [Fact]
    public async Task GetCopyJobRunStatusAsync_WithEmptyJobInstanceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.GetCopyJobRunStatusAsync(TestWorkspaceId, InvalidCopyJobId, "");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("jobInstanceId"));
    }

    #endregion

    #region CreateCopyJobScheduleAsync - Unauthenticated

    [Fact]
    public async Task CreateCopyJobScheduleAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange
        var configJson = "{\"type\":\"Cron\",\"startDateTime\":\"2024-04-28T00:00:00\",\"endDateTime\":\"2024-04-30T23:59:00\",\"localTimeZoneId\":\"Central Standard Time\",\"interval\":10}";

        // Act
        var result = await _copyJobTool.CreateCopyJobScheduleAsync(TestWorkspaceId, InvalidCopyJobId, true, configJson);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task CreateCopyJobScheduleAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Arrange
        var configJson = "{\"type\":\"Cron\",\"interval\":10}";

        // Act
        var result = await _copyJobTool.CreateCopyJobScheduleAsync("", InvalidCopyJobId, true, configJson);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task CreateCopyJobScheduleAsync_WithEmptyCopyJobId_ShouldReturnValidationError()
    {
        // Arrange
        var configJson = "{\"type\":\"Cron\",\"interval\":10}";

        // Act
        var result = await _copyJobTool.CreateCopyJobScheduleAsync(TestWorkspaceId, "", true, configJson);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("copyJobId"));
    }

    [Fact]
    public async Task CreateCopyJobScheduleAsync_WithEmptyConfigurationJson_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.CreateCopyJobScheduleAsync(TestWorkspaceId, InvalidCopyJobId, true, "");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("configurationJson"));
    }

    [Fact]
    public async Task CreateCopyJobScheduleAsync_WithInvalidConfigurationJson_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.CreateCopyJobScheduleAsync(TestWorkspaceId, InvalidCopyJobId, true, "not-valid-json{");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, "Invalid configuration JSON format");
    }

    #endregion

    #region ListCopyJobSchedulesAsync - Unauthenticated

    [Fact]
    public async Task ListCopyJobSchedulesAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _copyJobTool.ListCopyJobSchedulesAsync(TestWorkspaceId, InvalidCopyJobId);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListCopyJobSchedulesAsync_WithContinuationToken_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange
        var testToken = "test-continuation-token";

        // Act
        var result = await _copyJobTool.ListCopyJobSchedulesAsync(TestWorkspaceId, InvalidCopyJobId, testToken);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListCopyJobSchedulesAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.ListCopyJobSchedulesAsync("", InvalidCopyJobId);

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task ListCopyJobSchedulesAsync_WithEmptyCopyJobId_ShouldReturnValidationError()
    {
        // Act
        var result = await _copyJobTool.ListCopyJobSchedulesAsync(TestWorkspaceId, "");

        // Assert
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("copyJobId"));
    }

    #endregion

    #region Authenticated Scenarios

    [SkippableFact]
    public async Task ListCopyJobsAsync_WithAuthentication_ShouldReturnResultOrApiError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _copyJobTool.ListCopyJobsAsync(TestWorkspaceId);

        // Assert
        AssertCopyJobListResult(result);
    }

    [SkippableFact]
    public async Task ListCopyJobsAsync_WithInvalidWorkspaceId_ShouldReturnApiError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _copyJobTool.ListCopyJobsAsync(InvalidWorkspaceId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        AssertNoAuthenticationError(result);
        Assert.Contains("workspaceId must be a valid GUID", result);
    }

    [SkippableFact]
    public async Task GetCopyJobAsync_WithAuthentication_NonExistentCopyJob_ShouldReturnError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _copyJobTool.GetCopyJobAsync(TestWorkspaceId, InvalidCopyJobId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        SkipIfUpstreamBlocked(result);
        AssertNoAuthenticationError(result);
    }

    [SkippableFact]
    public async Task RunCopyJobAsync_WithAuthentication_NonExistentCopyJob_ShouldReturnError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _copyJobTool.RunCopyJobAsync(TestWorkspaceId, InvalidCopyJobId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        SkipIfUpstreamBlocked(result);
        AssertNoAuthenticationError(result);
    }

    [SkippableFact]
    public async Task GetCopyJobRunStatusAsync_WithAuthentication_NonExistentJobInstance_ShouldReturnError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _copyJobTool.GetCopyJobRunStatusAsync(TestWorkspaceId, InvalidCopyJobId, InvalidJobInstanceId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        SkipIfUpstreamBlocked(result);
        AssertNoAuthenticationError(result);
    }

    [SkippableFact]
    public async Task ListCopyJobSchedulesAsync_WithAuthentication_ShouldReturnResultOrApiError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _copyJobTool.ListCopyJobSchedulesAsync(TestWorkspaceId, InvalidCopyJobId);

        // Assert
        AssertScheduleListResult(result);
    }

    [SkippableFact]
    public async Task ListCopyJobSchedulesAsync_WithInvalidWorkspaceId_ShouldReturnApiError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _copyJobTool.ListCopyJobSchedulesAsync(InvalidWorkspaceId, InvalidCopyJobId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        AssertNoAuthenticationError(result);
        Assert.Contains("workspaceId must be a valid GUID", result);
    }

    #endregion

    #region Helper Methods

    private static void AssertCopyJobListResult(string result)
    {
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Skip if upstream service is blocking requests
        SkipIfUpstreamBlocked(result);

        AssertNoAuthenticationError(result);

        // Should either be "No copy jobs found" or valid JSON
        if (result.Contains("No copy jobs found"))
        {
            Assert.Contains("No copy jobs found", result);
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
            Assert.True(root.TryGetProperty("copyJobCount", out _), "JSON response should have copyJobCount property");
            Assert.True(root.TryGetProperty("copyJobs", out _), "JSON response should have copyJobs property");
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

            Assert.True(root.TryGetProperty("copyJobId", out _), "JSON response should have copyJobId property");
            Assert.True(root.TryGetProperty("workspaceId", out _), "JSON response should have workspaceId property");
            Assert.True(root.TryGetProperty("scheduleCount", out _), "JSON response should have scheduleCount property");
            Assert.True(root.TryGetProperty("schedules", out _), "JSON response should have schedules property");
        }
    }

    #endregion
}
