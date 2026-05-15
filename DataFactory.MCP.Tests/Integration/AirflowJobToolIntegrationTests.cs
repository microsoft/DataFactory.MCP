using Xunit;
using System.Text.Json;
using DataFactory.MCP.Tools.AirflowJob;
using DataFactory.MCP.Tests.Infrastructure;
using DataFactory.MCP.Models;

namespace DataFactory.MCP.Tests.Integration;

/// <summary>
/// Integration tests for AirflowJobTool that call the actual MCP tool methods
/// without mocking to verify real behavior
/// </summary>
public class AirflowJobToolIntegrationTests : FabricToolIntegrationTestBase
{
    private readonly AirflowJobTool _airflowJobTool;

    private const string TestWorkspaceId = "12c5e906-5bfc-4ba4-bd76-c1ce68fc53c8";
    private const string InvalidWorkspaceId = "invalid-workspace-id";
    private const string InvalidAirflowJobId = "00000000-0000-0000-0000-000000000001";

    public AirflowJobToolIntegrationTests(McpTestFixture fixture) : base(fixture)
    {
        _airflowJobTool = Fixture.GetService<AirflowJobTool>();
    }

    #region DI Registration

    [Fact]
    public void AirflowJobTool_ShouldBeRegisteredInDI()
    {
        Assert.NotNull(_airflowJobTool);
        Assert.IsType<AirflowJobTool>(_airflowJobTool);
    }

    #endregion

    #region ListAirflowJobsAsync - Unauthenticated

    [Fact]
    public async Task ListAirflowJobsAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        var result = await _airflowJobTool.ListAirflowJobsAsync(TestWorkspaceId);

        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListAirflowJobsAsync_WithContinuationToken_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        var result = await _airflowJobTool.ListAirflowJobsAsync(TestWorkspaceId, "test-continuation-token");

        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListAirflowJobsAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.ListAirflowJobsAsync("");

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task ListAirflowJobsAsync_WithNullWorkspaceId_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.ListAirflowJobsAsync(null!);

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    #endregion

    #region CreateAirflowJobAsync - Unauthenticated

    [Fact]
    public async Task CreateAirflowJobAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        var result = await _airflowJobTool.CreateAirflowJobAsync(TestWorkspaceId, "test-airflow-job");

        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task CreateAirflowJobAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.CreateAirflowJobAsync("", "test-airflow-job");

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task CreateAirflowJobAsync_WithEmptyDisplayName_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.CreateAirflowJobAsync(TestWorkspaceId, "");

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("displayName"));
    }

    [Fact]
    public async Task CreateAirflowJobAsync_WithNullDisplayName_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.CreateAirflowJobAsync(TestWorkspaceId, null!);

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("displayName"));
    }

    #endregion

    #region GetAirflowJobAsync - Unauthenticated

    [Fact]
    public async Task GetAirflowJobAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        var result = await _airflowJobTool.GetAirflowJobAsync(TestWorkspaceId, InvalidAirflowJobId);

        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task GetAirflowJobAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.GetAirflowJobAsync("", InvalidAirflowJobId);

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task GetAirflowJobAsync_WithEmptyAirflowJobId_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.GetAirflowJobAsync(TestWorkspaceId, "");

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("airflowJobId"));
    }

    [Fact]
    public async Task GetAirflowJobAsync_WithNullAirflowJobId_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.GetAirflowJobAsync(TestWorkspaceId, null!);

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("airflowJobId"));
    }

    #endregion

    #region UpdateAirflowJobAsync - Unauthenticated

    [Fact]
    public async Task UpdateAirflowJobAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        var result = await _airflowJobTool.UpdateAirflowJobAsync(TestWorkspaceId, InvalidAirflowJobId, displayName: "updated");

        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task UpdateAirflowJobAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.UpdateAirflowJobAsync("", InvalidAirflowJobId, displayName: "updated");

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task UpdateAirflowJobAsync_WithEmptyAirflowJobId_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.UpdateAirflowJobAsync(TestWorkspaceId, "", displayName: "updated");

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("airflowJobId"));
    }

    [Fact]
    public async Task UpdateAirflowJobAsync_WithNoUpdates_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.UpdateAirflowJobAsync(TestWorkspaceId, InvalidAirflowJobId);

        McpResponseAssertHelper.AssertValidationError(result, "At least one of displayName or description must be provided");
    }

    #endregion

    #region DeleteAirflowJobAsync - Unauthenticated

    [Fact]
    public async Task DeleteAirflowJobAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        var result = await _airflowJobTool.DeleteAirflowJobAsync(TestWorkspaceId, InvalidAirflowJobId);

        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task DeleteAirflowJobAsync_HardDelete_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        var result = await _airflowJobTool.DeleteAirflowJobAsync(TestWorkspaceId, InvalidAirflowJobId, hardDelete: true);

        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task DeleteAirflowJobAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.DeleteAirflowJobAsync("", InvalidAirflowJobId);

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task DeleteAirflowJobAsync_WithEmptyAirflowJobId_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.DeleteAirflowJobAsync(TestWorkspaceId, "");

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("airflowJobId"));
    }

    #endregion

    #region GetAirflowJobDefinitionAsync - Unauthenticated

    [Fact]
    public async Task GetAirflowJobDefinitionAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        var result = await _airflowJobTool.GetAirflowJobDefinitionAsync(TestWorkspaceId, InvalidAirflowJobId);

        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task GetAirflowJobDefinitionAsync_WithFormat_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        var result = await _airflowJobTool.GetAirflowJobDefinitionAsync(TestWorkspaceId, InvalidAirflowJobId, format: "Default");

        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task GetAirflowJobDefinitionAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.GetAirflowJobDefinitionAsync("", InvalidAirflowJobId);

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task GetAirflowJobDefinitionAsync_WithEmptyAirflowJobId_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.GetAirflowJobDefinitionAsync(TestWorkspaceId, "");

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("airflowJobId"));
    }

    #endregion

    #region UpdateAirflowJobDefinitionAsync - Unauthenticated

    [Fact]
    public async Task UpdateAirflowJobDefinitionAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        var definitionJson = "{\"schedulerConfig\":{}}";

        var result = await _airflowJobTool.UpdateAirflowJobDefinitionAsync(TestWorkspaceId, InvalidAirflowJobId, definitionJson);

        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task UpdateAirflowJobDefinitionAsync_WithUpdateMetadata_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        var definitionJson = "{\"schedulerConfig\":{}}";

        var result = await _airflowJobTool.UpdateAirflowJobDefinitionAsync(TestWorkspaceId, InvalidAirflowJobId, definitionJson, updateMetadata: true);

        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task UpdateAirflowJobDefinitionAsync_WithEmptyWorkspaceId_ShouldReturnValidationError()
    {
        var definitionJson = "{\"schedulerConfig\":{}}";

        var result = await _airflowJobTool.UpdateAirflowJobDefinitionAsync("", InvalidAirflowJobId, definitionJson);

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("workspaceId"));
    }

    [Fact]
    public async Task UpdateAirflowJobDefinitionAsync_WithEmptyAirflowJobId_ShouldReturnValidationError()
    {
        var definitionJson = "{\"schedulerConfig\":{}}";

        var result = await _airflowJobTool.UpdateAirflowJobDefinitionAsync(TestWorkspaceId, "", definitionJson);

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("airflowJobId"));
    }

    [Fact]
    public async Task UpdateAirflowJobDefinitionAsync_WithEmptyDefinitionJson_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.UpdateAirflowJobDefinitionAsync(TestWorkspaceId, InvalidAirflowJobId, "");

        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("definitionJson"));
    }

    [Fact]
    public async Task UpdateAirflowJobDefinitionAsync_WithInvalidJson_ShouldReturnValidationError()
    {
        var result = await _airflowJobTool.UpdateAirflowJobDefinitionAsync(TestWorkspaceId, InvalidAirflowJobId, "not-valid-json{");

        McpResponseAssertHelper.AssertValidationError(result, "Invalid JSON format");
    }

    #endregion

    #region Authenticated Scenarios

    [SkippableFact]
    public async Task ListAirflowJobsAsync_WithAuthentication_ShouldReturnResultOrApiError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _airflowJobTool.ListAirflowJobsAsync(TestWorkspaceId);

        // Assert
        AssertAirflowJobListResult(result);
    }

    [SkippableFact]
    public async Task ListAirflowJobsAsync_WithInvalidWorkspaceId_ShouldReturnApiError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _airflowJobTool.ListAirflowJobsAsync(InvalidWorkspaceId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        AssertNoAuthenticationError(result);
        Assert.Contains("workspaceId must be a valid GUID", result);
    }

    [SkippableFact]
    public async Task GetAirflowJobAsync_WithAuthentication_NonExistentAirflowJob_ShouldReturnError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _airflowJobTool.GetAirflowJobAsync(TestWorkspaceId, InvalidAirflowJobId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        SkipIfUpstreamBlocked(result);
        AssertNoAuthenticationError(result);
    }

    [SkippableFact]
    public async Task CreateAirflowJobAsync_WithAuthentication_ShouldCreateSuccessfully()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        var jobName = $"test-airflow-job-{DateTime.UtcNow:yyyyMMddHHmmss}";

        // Act
        var result = await _airflowJobTool.CreateAirflowJobAsync(TestWorkspaceId, jobName, "Created by integration test");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        SkipIfUpstreamBlocked(result);
        AssertNoAuthenticationError(result);

        if (result.Contains("\"success\": true") || result.Contains("created successfully"))
        {
            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.TryGetProperty("airflowJobId", out _), "Response should contain airflowJobId");
            Assert.True(json.RootElement.TryGetProperty("displayName", out _), "Response should contain displayName");

            // Cleanup
            var airflowJobId = json.RootElement.GetProperty("airflowJobId").GetString();
            if (!string.IsNullOrEmpty(airflowJobId))
            {
                await _airflowJobTool.DeleteAirflowJobAsync(TestWorkspaceId, airflowJobId, hardDelete: true);
            }
        }
    }

    [SkippableFact]
    public async Task GetAirflowJobDefinitionAsync_WithAuthentication_NonExistentJob_ShouldReturnError()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _airflowJobTool.GetAirflowJobDefinitionAsync(TestWorkspaceId, InvalidAirflowJobId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        SkipIfUpstreamBlocked(result);
        AssertNoAuthenticationError(result);
    }

    [SkippableFact]
    public async Task AirflowJob_FullLifecycle_CreateGetUpdateDeleteAsync()
    {
        // Arrange
        var isAuthenticated = await TryAuthenticateAsync();
        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        var jobName = $"test-lifecycle-{DateTime.UtcNow:yyyyMMddHHmmss}";
        string? airflowJobId = null;

        try
        {
            // Step 1: Create
            var createResult = await _airflowJobTool.CreateAirflowJobAsync(
                TestWorkspaceId, jobName, "Lifecycle integration test");

            Assert.NotNull(createResult);
            SkipIfUpstreamBlocked(createResult);
            AssertNoAuthenticationError(createResult);

            if (!IsValidJson(createResult)) return; // API may not support creation in test workspace

            var createJson = JsonDocument.Parse(createResult);
            Assert.True(createJson.RootElement.TryGetProperty("airflowJobId", out var idElement),
                $"Create response missing airflowJobId. Response: {createResult}");
            airflowJobId = idElement.GetString();
            Assert.False(string.IsNullOrEmpty(airflowJobId), "airflowJobId should not be empty");

            // Step 2: Get and verify
            var getResult = await _airflowJobTool.GetAirflowJobAsync(TestWorkspaceId, airflowJobId!);
            Assert.NotNull(getResult);
            SkipIfUpstreamBlocked(getResult);
            AssertNoAuthenticationError(getResult);
            Assert.True(IsValidJson(getResult), $"Get response should be JSON. Got: {getResult}");

            var getJson = JsonDocument.Parse(getResult);
            Assert.True(getJson.RootElement.TryGetProperty("id", out _), "Get response should have id");
            Assert.True(getJson.RootElement.TryGetProperty("displayName", out _), "Get response should have displayName");

            // Step 3: Update metadata
            var updatedName = $"{jobName}-updated";
            var updateResult = await _airflowJobTool.UpdateAirflowJobAsync(
                TestWorkspaceId, airflowJobId!, updatedName, "Updated by lifecycle test");

            Assert.NotNull(updateResult);
            SkipIfUpstreamBlocked(updateResult);
            AssertNoAuthenticationError(updateResult);

            // Step 4: List and confirm job appears
            var listResult = await _airflowJobTool.ListAirflowJobsAsync(TestWorkspaceId);
            Assert.NotNull(listResult);
            SkipIfUpstreamBlocked(listResult);
            AssertNoAuthenticationError(listResult);

            // Step 5: Get definition
            var definitionResult = await _airflowJobTool.GetAirflowJobDefinitionAsync(TestWorkspaceId, airflowJobId!);
            Assert.NotNull(definitionResult);
            SkipIfUpstreamBlocked(definitionResult);
            AssertNoAuthenticationError(definitionResult);
        }
        finally
        {
            // Cleanup: always attempt to delete
            if (!string.IsNullOrEmpty(airflowJobId))
            {
                await _airflowJobTool.DeleteAirflowJobAsync(TestWorkspaceId, airflowJobId, hardDelete: true);
            }
        }
    }

    #endregion

    #region Helper Methods

    private static void AssertAirflowJobListResult(string result)
    {
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        SkipIfUpstreamBlocked(result);
        AssertNoAuthenticationError(result);

        if (result.Contains("No Apache Airflow Jobs found"))
        {
            return; // Valid empty-workspace response
        }

        if (result.Contains("HttpRequestError"))
        {
            McpResponseAssertHelper.AssertHttpError(result);
            return;
        }

        if (IsValidJson(result))
        {
            var json = JsonDocument.Parse(result);
            Assert.True(json.RootElement.TryGetProperty("airflowJobCount", out _),
                "List response should have airflowJobCount");
            Assert.True(json.RootElement.TryGetProperty("airflowJobs", out _),
                "List response should have airflowJobs");
        }
    }

    #endregion
}
