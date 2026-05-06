using Xunit;
using Fabric.Mcp.Tools.DataFactory.Tools;
using DataFactory.MCP.Tests.Infrastructure;
using Fabric.Mcp.Tools.DataFactory.Models;

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
        // Arrange - use valid GUID format so validation passes and auth is checked
        var testConnectionId = "00000000-0000-0000-0000-000000000001";

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
        McpResponseAssertHelper.AssertValidationError(result, Messages.InvalidParameterEmpty("connectionId"));
    }

    [Fact]
    public void ConnectionsTool_ShouldBeRegisteredInDI()
    {
        // Assert
        Assert.NotNull(_connectionsTool);
        Assert.IsType<ConnectionsTool>(_connectionsTool);
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
        AssertResult("connections", result);
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
        AssertResult("connections", result);
    }

    [SkippableFact]
    public async Task GetConnectionAsync_WithAuthentication_AndValidId_ShouldNotReturnAuthenticationError()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Use a valid GUID format that doesn't exist in the system
        var testConnectionId = "00000000-0000-0000-0000-000000000001";

        // Act
        var result = await _connectionsTool.GetConnectionAsync(testConnectionId);

        // Assert
        AssertOneItemResult("Connection", testConnectionId, result);
    }

    [Fact]
    public async Task GetConnectionAsync_WithInvalidGuidFormat_ShouldReturnValidationError()
    {
        // Arrange - invalid GUID format
        var testConnectionId = "test-connection-id-12345";

        // Act
        var result = await _connectionsTool.GetConnectionAsync(testConnectionId);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("ValidationError", result);
    }

    #endregion
}