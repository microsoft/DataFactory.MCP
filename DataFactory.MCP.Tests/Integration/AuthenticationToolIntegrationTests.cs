using Xunit;
using Fabric.Mcp.Tools.DataFactory.Tools;
using DataFactory.MCP.Tests.Infrastructure;
using Fabric.Mcp.Tools.DataFactory.Models;

namespace DataFactory.MCP.Tests.Integration;

/// <summary>
/// Integration tests for AuthenticationTool that call the actual MCP tool methods
/// without mocking to verify real behavior
/// </summary>
public class AuthenticationToolIntegrationTests : FabricToolIntegrationTestBase
{
    private readonly AuthenticationTool _authTool;

    public AuthenticationToolIntegrationTests(McpTestFixture fixture) : base(fixture)
    {
        _authTool = fixture.GetService<AuthenticationTool>();
    }

    [Fact]
    public void GetAuthenticationStatus_ShouldReturnValidResponse()
    {
        // Act
        var result = _authTool.GetAuthenticationStatus();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Should contain either authentication status information or an error message
        Assert.Equal(Messages.NotAuthenticated, result);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WithoutAuthentication_ShouldReturnErrorMessage()
    {
        // Act
        var result = await _authTool.GetAccessTokenAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Should return an error since we're not authenticated
        Assert.Equal(Messages.NoAuthenticationFound, result);
    }

    [Fact]
    public async Task SignOutAsync_ShouldReturnValidResponse()
    {
        // Act
        var result = await _authTool.SignOutAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Should return either success message or error
        Assert.True(
            result.Contains("Successfully signed out user") ||
            result.Contains("No active authentication session found.")
        );
    }

    [SkippableFact]
    public async Task AuthenticateServicePrincipalAsync_WithRealCredentials_ShouldSucceed()
    {
        // Arrange - Try to authenticate
        var isAuthenticated = await TryAuthenticateAsync();

        Skip.IfNot(isAuthenticated, "Skipping authenticated test - no valid credentials available");

        // Arrange - using real service principal credentials from environment
        var realAppId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? "#{AZURE_CLIENT_ID}#";
        var realSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET") ?? "#{AZURE_CLIENT_SECRET}#";
        var realTenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID") ?? "#{AZURE_TENANT_ID}#";

        // Act
        var result = await _authTool.AuthenticateServicePrincipalAsync(realAppId, realSecret, realTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        Assert.Equal(string.Format(Messages.ServicePrincipalAuthenticationSuccessTemplate, realAppId), result);
    }

}
