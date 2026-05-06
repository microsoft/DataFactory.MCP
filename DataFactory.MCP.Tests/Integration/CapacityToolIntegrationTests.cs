using Xunit;
using Fabric.Mcp.Tools.DataFactory.Tools;
using DataFactory.MCP.Tests.Infrastructure;
using System.Text.Json;

namespace DataFactory.MCP.Tests.Integration;

/// <summary>
/// Integration tests for CapacityTool that call the actual MCP tool methods
/// without mocking to verify real behavior
/// </summary>
public class CapacityToolIntegrationTests : FabricToolIntegrationTestBase
{
    private readonly CapacityTool _capacityTool;

    public CapacityToolIntegrationTests(McpTestFixture fixture) : base(fixture)
    {
        _capacityTool = Fixture.GetService<CapacityTool>();
    }

    [Fact]
    public async Task ListCapacitiesAsync_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Act
        var result = await _capacityTool.ListCapacitiesAsync();

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public async Task ListCapacitiesAsync_WithContinuationToken_WithoutAuthentication_ShouldReturnAuthenticationError()
    {
        // Arrange
        var testToken = "test-continuation-token";

        // Act
        var result = await _capacityTool.ListCapacitiesAsync(testToken);

        // Assert
        AssertAuthenticationError(result);
    }

    [Fact]
    public void CapacityTool_ShouldBeRegisteredInDI()
    {
        // Assert
        Assert.NotNull(_capacityTool);
        Assert.IsType<CapacityTool>(_capacityTool);
    }

    #region Authenticated Scenarios

    /// <summary>
    /// Test listing capacities with authentication
    /// </summary>
    [SkippableFact]
    public async Task ListCapacitiesAsync_WithAuthentication_ShouldReturnJsonResponseOrNoCapacitiesMessage()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Act
        var result = await _capacityTool.ListCapacitiesAsync();

        // Assert
        AssertCapacityResult(result);
    }

    [SkippableFact]
    public async Task ListCapacitiesAsync_WithAuthentication_AndContinuationToken_ShouldHandleTokenParameter()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        var testToken = "test-continuation-token-12345";

        // Act
        var result = await _capacityTool.ListCapacitiesAsync(testToken);

        // Assert
        AssertCapacityResult(result);
    }

    #endregion

    #region Helper Methods

    private static void AssertCapacityResult(string result)
    {
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Skip if upstream service is blocking requests
        SkipIfUpstreamBlocked(result);

        AssertNoAuthenticationError(result);

        // Should either be a valid response, "no capacities found", or API error
        Assert.True(
            result.Contains("No capacities found") ||
            IsValidJson(result) ||
            result.Contains("API request failed"),
            $"Expected valid response format, got: {result}");

        // If it's JSON, verify basic structure
        if (IsValidJson(result))
        {
            var jsonDoc = JsonDocument.Parse(result);
            var root = jsonDoc.RootElement;

            Assert.True(root.TryGetProperty("totalCount", out _), "JSON response should have totalCount property");
            Assert.True(root.TryGetProperty("hasMoreResults", out _), "JSON response should have hasMoreResults property");
            Assert.True(root.TryGetProperty("formattedResults", out _), "JSON response should have formattedResults property");

            // Verify formattedResults structure
            if (root.TryGetProperty("formattedResults", out var formattedResults))
            {
                Assert.True(formattedResults.TryGetProperty("capacities", out _), "formattedResults should have capacities property");
                Assert.True(formattedResults.TryGetProperty("summary", out _), "formattedResults should have summary property");

                // Verify summary structure
                if (formattedResults.TryGetProperty("summary", out var summary))
                {
                    Assert.True(summary.TryGetProperty("totalCount", out _), "summary should have totalCount property");
                    Assert.True(summary.TryGetProperty("byState", out _), "summary should have byState property");
                    Assert.True(summary.TryGetProperty("bySku", out _), "summary should have bySku property");
                    Assert.True(summary.TryGetProperty("byRegion", out _), "summary should have byRegion property");
                    Assert.True(summary.TryGetProperty("activeCount", out _), "summary should have activeCount property");
                    Assert.True(summary.TryGetProperty("inactiveCount", out _), "summary should have inactiveCount property");
                }

                // Verify capacities array structure if present
                if (formattedResults.TryGetProperty("capacities", out var capacities) && capacities.ValueKind == JsonValueKind.Array)
                {
                    foreach (var capacity in capacities.EnumerateArray())
                    {
                        Assert.True(capacity.TryGetProperty("id", out _), "capacity should have id property");
                        Assert.True(capacity.TryGetProperty("displayName", out _), "capacity should have displayName property");
                        Assert.True(capacity.TryGetProperty("sku", out _), "capacity should have sku property");
                        Assert.True(capacity.TryGetProperty("skuDescription", out _), "capacity should have skuDescription property");
                        Assert.True(capacity.TryGetProperty("region", out _), "capacity should have region property");
                        Assert.True(capacity.TryGetProperty("state", out _), "capacity should have state property");
                        Assert.True(capacity.TryGetProperty("status", out _), "capacity should have status property");
                        Assert.True(capacity.TryGetProperty("isActive", out _), "capacity should have isActive property");
                    }
                }
            }
        }
    }

    #endregion
}