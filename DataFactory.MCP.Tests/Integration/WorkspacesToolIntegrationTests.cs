using Xunit;
using DataFactory.MCP.Tools;
using DataFactory.MCP.Tests.Infrastructure;
using DataFactory.MCP.Models;
using System.Text.Json;

namespace DataFactory.MCP.Tests.Integration;

/// <summary>
/// Integration tests for WorkspacesTool that call the actual MCP tool methods
/// without mocking to verify real behavior
/// </summary>
public class WorkspacesToolIntegrationTests : FabricToolIntegrationTestBase
{
    private readonly WorkspacesTool _workspacesTool;

    public WorkspacesToolIntegrationTests(McpTestFixture fixture) : base(fixture)
    {
        _workspacesTool = Fixture.GetService<WorkspacesTool>();
    }

    [Fact]
    public async Task ListWorkspacesAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _workspacesTool.ListWorkspacesAsync();

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListWorkspacesAsync_WithRoles_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange
        var testRoles = "Admin,Member";

        // Act
        var result = await _workspacesTool.ListWorkspacesAsync(testRoles);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListWorkspacesAsync_WithContinuationToken_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange
        var testToken = "test-continuation-token";

        // Act
        var result = await _workspacesTool.ListWorkspacesAsync(continuationToken: testToken);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListWorkspacesAsync_WithPreferWorkspaceSpecificEndpoints_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _workspacesTool.ListWorkspacesAsync(preferWorkspaceSpecificEndpoints: true);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public void WorkspacesTool_ShouldBeRegisteredInDI()
    {
        // Assert
        Assert.NotNull(_workspacesTool);
        Assert.IsType<WorkspacesTool>(_workspacesTool);
    }

    #region Authenticated Scenarios

    /// <summary>
    /// Test listing workspaces with authentication
    /// </summary>
    [SkippableFact]
    public async Task ListWorkspacesAsync_WithAuthentication_ShouldReturnJsonResponseOrNoWorkspacesMessage()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _workspacesTool.ListWorkspacesAsync();

        // Assert
        AssertResult("workspaces", result);
    }

    [SkippableFact]
    public async Task ListWorkspacesAsync_WithAuthentication_AndRoles_ShouldReturnJsonResponseOrNoWorkspacesMessage()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        var testRoles = "Admin,Member,Contributor,Viewer";

        // Act
        var result = await _workspacesTool.ListWorkspacesAsync(testRoles);

        // Assert
        AssertResult("workspaces", result);

        // Additional check for roles filtering
        if (IsValidJson(result) && !result.Contains("No workspaces found"))
        {
            var jsonDoc = JsonDocument.Parse(result);
            Assert.True(jsonDoc.RootElement.TryGetProperty("filteredByRoles", out var filteredByRoles));
            Assert.True(filteredByRoles.GetBoolean());
            Assert.True(jsonDoc.RootElement.TryGetProperty("roles", out var roles));
            Assert.Equal(testRoles, roles.GetString());
        }
    }

    [SkippableFact]
    public async Task ListWorkspacesAsync_WithAuthentication_AndContinuationToken_ShouldHandleTokenParameter()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        var testToken = "test-continuation-token-12345";

        // Act
        var result = await _workspacesTool.ListWorkspacesAsync(continuationToken: testToken);

        // Assert
        AssertResult("workspaces", result);
    }

    [SkippableFact]
    public async Task ListWorkspacesAsync_WithAuthentication_AndPreferWorkspaceSpecificEndpoints_ShouldIncludeApiEndpoints()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _workspacesTool.ListWorkspacesAsync(preferWorkspaceSpecificEndpoints: true);

        // Assert
        AssertResult("workspaces", result);

        // Additional check for API endpoints flag
        if (IsValidJson(result) && !result.Contains("No workspaces found"))
        {
            var jsonDoc = JsonDocument.Parse(result);
            Assert.True(jsonDoc.RootElement.TryGetProperty("includesApiEndpoints", out var includesApiEndpoints));
            Assert.True(includesApiEndpoints.GetBoolean());
        }
    }

    [SkippableFact]
    public async Task ListWorkspacesAsync_WithAuthentication_AllParameters_ShouldHandleComplexRequest()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        var testRoles = "Admin,Contributor";
        var testToken = "test-token-complex";

        // Act
        var result = await _workspacesTool.ListWorkspacesAsync(
            roles: testRoles,
            continuationToken: testToken,
            preferWorkspaceSpecificEndpoints: true);

        // Assert
        AssertResult("workspaces", result);

        // Additional checks for all parameters
        if (IsValidJson(result) && !result.Contains("No workspaces found"))
        {
            var jsonDoc = JsonDocument.Parse(result);

            // Check roles filtering
            Assert.True(jsonDoc.RootElement.TryGetProperty("filteredByRoles", out var filteredByRoles));
            Assert.True(filteredByRoles.GetBoolean());
            Assert.True(jsonDoc.RootElement.TryGetProperty("roles", out var roles));
            Assert.Equal(testRoles, roles.GetString());

            // Check API endpoints inclusion
            Assert.True(jsonDoc.RootElement.TryGetProperty("includesApiEndpoints", out var includesApiEndpoints));
            Assert.True(includesApiEndpoints.GetBoolean());
        }
    }

    #endregion
}