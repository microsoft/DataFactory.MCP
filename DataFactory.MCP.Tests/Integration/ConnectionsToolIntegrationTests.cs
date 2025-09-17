using Xunit;
using DataFactory.MCP.Tools;
using DataFactory.MCP.Tests.Infrastructure;
using DataFactory.MCP.Models;
using System.Text.Json;

namespace DataFactory.MCP.Tests.Integration;

/// <summary>
/// Integration tests for ConnectionsTool that call the actual MCP tool methods
/// without mocking to verify real behavior
/// </summary>
public class ConnectionsToolIntegrationTests : FabricToolIntegrationTestBase
{
    private readonly ConnectionsTool _connectionsTool;

    public ConnectionsToolIntegrationTests(McpTestFixture fixture) : base(fixture)
    {
        _connectionsTool = Fixture.GetService<ConnectionsTool>();
    }

    [Fact]
    public async Task ListConnectionsAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _connectionsTool.ListConnectionsAsync();

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListConnectionsAsync_WithContinuationToken_ShouldHandleTokenParameter()
    {
        // Arrange
        var testToken = "test-continuation-token";

        // Act
        var result = await _connectionsTool.ListConnectionsAsync(testToken);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task GetConnectionAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange
        var testConnectionId = "test-connection-id";

        // Act
        var result = await _connectionsTool.GetConnectionAsync(testConnectionId);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task GetConnectionAsync_WithEmptyConnectionId_ShouldReturnValidationError()
    {
        // Act
        var result = await _connectionsTool.GetConnectionAsync("");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Connection ID is required.", result);
    }

    [Fact]
    public void ConnectionsTool_ShouldBeRegisteredInDI()
    {
        // Assert
        Assert.NotNull(_connectionsTool);
        Assert.IsType<ConnectionsTool>(_connectionsTool);
    }

    #region Authenticated Scenarios

    private static void AssertConnectionResult(string result)
    {

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Skip if upstream service is blocking requests
        SkipIfUpstreamBlocked(result);

        // Should not contain authentication error when properly authenticated
        Assert.DoesNotContain("Authentication error", result);
        Assert.DoesNotContain("authentication required", result, StringComparison.OrdinalIgnoreCase);

        if (result.Contains("No connections found"))
        {
            // If no connections, that's a valid response
            Assert.Contains("No connections found", result);
            return;
        }


        var jsonDoc = JsonDocument.Parse(result);

        // If it's JSON, verify it has the expected structure
        Assert.True(jsonDoc.RootElement.TryGetProperty("totalCount", out _), "JSON response should have totalCount property");
        Assert.True(jsonDoc.RootElement.TryGetProperty("connections", out _), "JSON response should have connections property");

    }

    /// <summary>
    /// Helper method to authenticate using environment variables if available
    /// </summary>
    [SkippableFact]
    public async Task ListConnectionsAsync_WithAuthentication_ShouldReturnJsonResponseOrNoConnectionsMessage()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _connectionsTool.ListConnectionsAsync();

        // Assert
        AssertConnectionResult(result);
    }

    [SkippableFact]
    public async Task ListConnectionsAsync_WithAuthentication_AndContinuationToken_ShouldHandleTokenParameter()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        var testToken = "test-continuation-token-12345";

        // Act
        var result = await _connectionsTool.ListConnectionsAsync(testToken);

        // Assert
        AssertConnectionResult(result);
    }

    [SkippableFact]
    public async Task GetConnectionAsync_WithAuthentication_AndValidId_ShouldNotReturnAuthenticationError()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        var testConnectionId = "test-connection-id-12345";

        // Act
        var result = await _connectionsTool.GetConnectionAsync(testConnectionId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Skip if upstream service is blocking requests
        SkipIfUpstreamBlocked(result);

        // The result should be either a "not found" message or connection details
        Assert.Equal($"Connection with ID '{testConnectionId}' not found or you don't have permission to access it.", result);
    }

    #endregion
}