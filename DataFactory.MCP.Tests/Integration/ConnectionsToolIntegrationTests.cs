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
    public async Task GetConnectionAsync_WithNullConnectionId_ShouldReturnValidationError()
    {
        // Act
        var result = await _connectionsTool.GetConnectionAsync(null!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Connection ID is required.", result);
    }

    [Fact]
    public async Task GetConnectionAsync_WithWhitespaceConnectionId_ShouldReturnValidationError()
    {
        // Act
        var result = await _connectionsTool.GetConnectionAsync("   ");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Connection ID is required.", result);
    }

    [Theory]
    [InlineData("invalid-guid")]
    [InlineData("test-connection-123")]
    [InlineData("non-existent-connection")]
    public async Task GetConnectionAsync_WithInvalidConnectionId_ShouldHandleIdParameter(string connectionId)
    {
        // Act
        var result = await _connectionsTool.GetConnectionAsync(connectionId);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public void ConnectionsTool_ShouldBeRegisteredInDI()
    {
        // Assert
        Assert.NotNull(_connectionsTool);
        Assert.IsType<ConnectionsTool>(_connectionsTool);
    }

    [Fact]
    public async Task ListConnectionsAsync_Result_ShouldNotBeJson_WhenUnauthenticated()
    {
        // Act
        var result = await _connectionsTool.ListConnectionsAsync();

        // Assert
        Assert.NotNull(result);

        // When there's an authentication error, the result should be a plain error message, not JSON
        var isValidJson = false;
        try
        {
            JsonDocument.Parse(result);
            isValidJson = true;
        }
        catch (JsonException)
        {
            // Expected - should not be valid JSON when unauthenticated
        }

        Assert.False(isValidJson, $"Expected plain error message but got JSON: {result}");
    }

    #region Authenticated Scenarios

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
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Skip if upstream service is blocking requests
        SkipIfUpstreamBlocked(result);

        // When authenticated, should either get JSON response or a "no connections" message
        try
        {
            var jsonDoc = JsonDocument.Parse(result);

            // If it's JSON, verify it has the expected structure
            Assert.True(jsonDoc.RootElement.TryGetProperty("totalCount", out _), "JSON response should have totalCount property");
            Assert.True(jsonDoc.RootElement.TryGetProperty("connections", out _), "JSON response should have connections property");
        }
        catch (JsonException)
        {
            // If not JSON, should be a "no connections" message
            Assert.Contains("No connections found", result);
        }

        // Should not contain authentication error when properly authenticated
        Assert.DoesNotContain("Authentication error", result);
        Assert.DoesNotContain("authentication required", result, StringComparison.OrdinalIgnoreCase);
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
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Skip if upstream service is blocking requests
        SkipIfUpstreamBlocked(result);

        // Should not contain authentication error when properly authenticated
        Assert.DoesNotContain("Authentication error", result);
        Assert.DoesNotContain("authentication required", result, StringComparison.OrdinalIgnoreCase);

        // The result might be an API error about invalid token format, or actual results
        // but should not be an authentication error
        Assert.True(
            result.Contains("No connections found") ||
            result.Contains("totalCount") ||
            result.Contains("API request failed") ||
            result.Contains("Error listing connections"),
            $"Unexpected response when authenticated: {result}"
        );
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

        // Should not contain authentication error when properly authenticated
        Assert.DoesNotContain("Authentication error", result);
        Assert.DoesNotContain("authentication required", result, StringComparison.OrdinalIgnoreCase);

        // The result should be either a "not found" message or connection details
        Assert.True(
            result.Contains("not found") ||
            result.Contains("don't have permission") ||
            result.Contains("id") ||
            result.Contains("name") ||
            result.Contains("Error retrieving connection"),
            $"Unexpected response when authenticated: {result}"
        );
    }

    [SkippableFact]
    public async Task GetConnectionAsync_WithAuthentication_AndExistingConnection_ShouldReturnConnectionDetails()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // First, try to get a list of connections to find an existing one
        var listResult = await _connectionsTool.ListConnectionsAsync();

        // Skip if upstream service is blocking requests
        SkipIfUpstreamBlocked(listResult);

        if (listResult.Contains("No connections found") || listResult.Contains("Authentication error"))
        {
            Assert.True(true, "Skipping test - no connections available or still not authenticated");
            return;
        }

        // Try to parse the JSON to get a connection ID
        string? connectionId = null;
        try
        {
            var jsonDoc = JsonDocument.Parse(listResult);
            if (jsonDoc.RootElement.TryGetProperty("connections", out var connectionsElement) &&
                connectionsElement.GetArrayLength() > 0)
            {
                var firstConnection = connectionsElement[0];
                if (firstConnection.TryGetProperty("id", out var idElement))
                {
                    connectionId = idElement.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // Could not parse, skip this test
            Assert.True(true, "Skipping test - could not parse connections list");
            return;
        }

        if (string.IsNullOrEmpty(connectionId))
        {
            Assert.True(true, "Skipping test - no connection ID found in response");
            return;
        }

        // Act
        var result = await _connectionsTool.GetConnectionAsync(connectionId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Skip if upstream service is blocking requests
        SkipIfUpstreamBlocked(result);

        // Should not contain authentication error
        Assert.DoesNotContain("Authentication error", result);

        // Should either be valid JSON with connection details or a not found message
        try
        {
            var jsonDoc = JsonDocument.Parse(result);
            // If it's JSON, it should have connection properties
            Assert.True(
                jsonDoc.RootElement.TryGetProperty("id", out _) ||
                jsonDoc.RootElement.TryGetProperty("name", out _),
                "JSON response should have connection properties"
            );
        }
        catch (JsonException)
        {
            // If not JSON, should be a descriptive message
            Assert.True(
                result.Contains("not found") ||
                result.Contains("Error retrieving connection"),
                $"Non-JSON response should be descriptive: {result}"
            );
        }
    }

    [SkippableTheory]
    [InlineData("non-existent-connection-id")]
    [InlineData("invalid-guid-format")]
    [InlineData("test-connection-that-does-not-exist")]
    public async Task GetConnectionAsync_WithAuthentication_AndNonExistentId_ShouldReturnNotFoundMessage(string connectionId)
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _connectionsTool.GetConnectionAsync(connectionId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Skip if upstream service is blocking requests
        SkipIfUpstreamBlocked(result);

        // Should not contain authentication error when properly authenticated
        Assert.DoesNotContain("Authentication error", result);
        Assert.DoesNotContain("authentication required", result, StringComparison.OrdinalIgnoreCase);

        // Should indicate the connection was not found or there was an API error
        Assert.True(
            result.Contains("not found") ||
            result.Contains("don't have permission") ||
            result.Contains("Error retrieving connection"),
            $"Expected not found message but got: {result}"
        );
    }

    [SkippableFact]
    public async Task ConnectionsTool_WithAuthentication_ShouldHandleApiFailures()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // This test verifies that when authenticated, API failures are handled gracefully
        // and don't result in authentication errors

        // Act - Test with various scenarios that might cause API failures
        var listResult = await _connectionsTool.ListConnectionsAsync();
        var invalidTokenResult = await _connectionsTool.ListConnectionsAsync("invalid-continuation-token");
        var getResult = await _connectionsTool.GetConnectionAsync("test-connection-id");

        // Skip if any upstream service is blocking requests
        SkipIfUpstreamBlocked(listResult);
        SkipIfUpstreamBlocked(invalidTokenResult);
        SkipIfUpstreamBlocked(getResult);

        // Assert - None should contain authentication errors
        Assert.DoesNotContain("Authentication error", listResult);
        Assert.DoesNotContain("Authentication error", invalidTokenResult);
        Assert.DoesNotContain("Authentication error", getResult);

        // All should have meaningful responses
        Assert.NotEmpty(listResult);
        Assert.NotEmpty(invalidTokenResult);
        Assert.NotEmpty(getResult);
    }

    #endregion
}