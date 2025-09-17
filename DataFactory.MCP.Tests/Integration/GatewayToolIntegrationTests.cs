using Xunit;
using DataFactory.MCP.Tools;
using DataFactory.MCP.Tests.Infrastructure;
using DataFactory.MCP.Models;
using System.Text.Json;

namespace DataFactory.MCP.Tests.Integration;

/// <summary>
/// Integration tests for GatewayTool that call the actual MCP tool methods
/// without mocking to verify real behavior
/// </summary>
public class GatewayToolIntegrationTests : FabricToolIntegrationTestBase
{
    private readonly GatewayTool _gatewayTool;

    public GatewayToolIntegrationTests(McpTestFixture fixture) : base(fixture)
    {
        _gatewayTool = Fixture.GetService<GatewayTool>();
    }

    [Fact]
    public async Task ListGatewaysAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _gatewayTool.ListGatewaysAsync();

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListGatewaysAsync_WithContinuationToken_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange
        var testToken = "test-continuation-token";

        // Act
        var result = await _gatewayTool.ListGatewaysAsync(testToken);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task GetGatewayAsync_WithEmptyGatewayId_ShouldReturnValidationError()
    {
        // Test empty string
        var result1 = await _gatewayTool.GetGatewayAsync("");
        Assert.Contains("Gateway ID is required", result1);

        // Test whitespace
        var result2 = await _gatewayTool.GetGatewayAsync("   ");
        Assert.Contains("Gateway ID is required", result2);

        // Test null (will be handled by method signature, but let's test if it gets to validation)
        var result3 = await _gatewayTool.GetGatewayAsync(null!);
        Assert.Contains("Gateway ID is required", result3);
    }

    [Fact]
    public async Task GetGatewayAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange - using a valid GUID format that won't exist
        var testGatewayId = "12345678-1234-1234-1234-123456789abc";

        // Act
        var result = await _gatewayTool.GetGatewayAsync(testGatewayId);

        // Assert
        AssertAuthenticationError(result);
    }

    [Theory]
    [InlineData("gateway-1")]
    [InlineData("12345")]
    [InlineData("test-gateway-id")]
    [InlineData("a1b2c3d4-e5f6-7890-abcd-ef1234567890")]
    public async Task GetGatewayAsync_WithVariousGatewayIdFormats_WithoutAuthentication_ShouldReturnAuthenticationError(string gatewayId)
    {
        // Act
        var result = await _gatewayTool.GetGatewayAsync(gatewayId);

        // Assert
        AssertAuthenticationError(result);
    }

    [SkippableFact]
    public async Task ListGatewaysAsync_WithAuthentication_ShouldReturnValidResponse()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _gatewayTool.ListGatewaysAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Skip if upstream service is blocking requests
        SkipIfUpstreamBlocked(result);

        AssertNoAuthenticationError(result);

        // Should either be valid JSON with gateway data or a "no gateways" message
        AssertValidResponseFormat(result,
            new[] { "totalCount", "TotalCount", "gateways" },
            new[] { "No gateways found", "Error listing gateways" });
    }

    [SkippableFact]
    public async Task ListGatewaysAsync_WithAuthentication_AndContinuationToken_ShouldHandleTokenParameter()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        var testToken = "test-continuation-token-12345";

        // Act
        var result = await _gatewayTool.ListGatewaysAsync(testToken);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Skip if upstream service is blocking requests
        SkipIfUpstreamBlocked(result);

        AssertNoAuthenticationError(result);

        // The result might be an API error about invalid token format, or actual results
        // but should not be an authentication error
        Assert.True(
            result.Contains("No gateways found") ||
            result.Contains("totalCount") ||
            result.Contains("API request failed") ||
            result.Contains("Error listing gateways"),
            $"Unexpected response when authenticated: {result}"
        );
    }

    [SkippableFact]
    public async Task GetGatewayAsync_WithAuthentication_AndValidId_ShouldNotReturnAuthenticationError()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        var testGatewayId = "test-gateway-id-12345";

        // Act
        var result = await _gatewayTool.GetGatewayAsync(testGatewayId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        AssertNoAuthenticationError(result);

        // The result should be either a "not found" message or gateway details
        Assert.True(
            result.Contains("not found") ||
            result.Contains("don't have permission") ||
            result.Contains("id") ||
            result.Contains("name") ||
            result.Contains("Error retrieving gateway"),
            $"Unexpected response when authenticated: {result}"
        );
    }

    [SkippableTheory]
    [InlineData("non-existent-gateway-id")]
    [InlineData("invalid-guid-format")]
    [InlineData("test-gateway-that-does-not-exist")]
    public async Task GetGatewayAsync_WithAuthentication_AndNonExistentId_ShouldReturnNotFoundMessage(string gatewayId)
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _gatewayTool.GetGatewayAsync(gatewayId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        AssertNoAuthenticationError(result);

        // Should indicate the gateway was not found or there was an API error
        Assert.True(
            result.Contains("not found") ||
            result.Contains("don't have permission") ||
            result.Contains("Error retrieving gateway"),
            $"Expected not found message but got: {result}"
        );
    }

    [Fact]
    public void GatewayTool_ShouldBeRegisteredInDI()
    {
        // Assert
        Assert.NotNull(_gatewayTool);
        Assert.IsType<GatewayTool>(_gatewayTool);
    }

    [SkippableFact]
    public async Task GatewayTool_WithAuthentication_ShouldHandleApiFailures()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // This test verifies that when authenticated, API failures are handled gracefully
        // and don't result in authentication errors

        // Act - Test with various scenarios that might cause API failures
        var listResult = await _gatewayTool.ListGatewaysAsync();
        var invalidTokenResult = await _gatewayTool.ListGatewaysAsync("invalid-continuation-token");
        var getResult = await _gatewayTool.GetGatewayAsync("test-gateway-id");

        // Assert - None should contain authentication errors
        AssertNoAuthenticationError(listResult);
        AssertNoAuthenticationError(invalidTokenResult);
        AssertNoAuthenticationError(getResult);

        // All should have meaningful responses
        Assert.NotEmpty(listResult);
        Assert.NotEmpty(invalidTokenResult);
        Assert.NotEmpty(getResult);
    }

}