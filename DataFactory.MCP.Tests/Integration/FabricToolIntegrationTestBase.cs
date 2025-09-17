using Xunit;
using DataFactory.MCP.Tools;
using DataFactory.MCP.Tests.Infrastructure;
using DataFactory.MCP.Models;
using System.Text.Json;

namespace DataFactory.MCP.Tests.Integration;

/// <summary>
/// Base class for Fabric tool integration tests providing common authentication 
/// and assertion functionality
/// </summary>
public abstract class FabricToolIntegrationTestBase : IClassFixture<McpTestFixture>
{
    protected readonly McpTestFixture Fixture;
    private readonly AuthenticationTool _authTool;

    protected FabricToolIntegrationTestBase(McpTestFixture fixture)
    {
        Fixture = fixture;
        _authTool = Fixture.GetService<AuthenticationTool>();

        // Ensure we start with no authentication for unauthenticated tests
        _authTool.SignOutAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Assert that the result contains the expected authentication error message
    /// </summary>
    protected static void AssertAuthenticationError(string result)
    {
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains($"Authentication error: {ErrorMessages.AuthenticationRequired}", result);
    }

    /// <summary>
    /// Check if a string is valid JSON
    /// </summary>
    protected static bool IsValidJson(string jsonString)
    {
        try
        {
            JsonDocument.Parse(jsonString);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Helper method to authenticate using environment variables if available
    /// </summary>
    protected async Task<bool> TryAuthenticateAsync()
    {
        var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
        var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");

        // Skip authentication tests if environment variables are not set or are placeholder values
        if (string.IsNullOrEmpty(clientId) ||
            string.IsNullOrEmpty(clientSecret) ||
            string.IsNullOrEmpty(tenantId) ||
            clientId.Contains("#") ||
            clientSecret.Contains("#") ||
            tenantId.Contains("#"))
        {
            return false;
        }

        // If credentials are available, authentication must succeed - otherwise fail the test
        var result = await _authTool.AuthenticateServicePrincipalAsync(clientId, clientSecret, tenantId);
        var success = result.Contains("successfully") || result.Contains("completed successfully");

        if (!success)
        {
            var errorMessage = $"Authentication failed with available credentials. " +
                             $"Client ID: {clientId}, Tenant ID: {tenantId}, " +
                             $"Error: {result}";

            Assert.Fail(errorMessage);
        }

        return true;
    }

    /// <summary>
    /// Assert that the result does not contain authentication errors when authenticated
    /// </summary>
    protected static void AssertNoAuthenticationError(string result)
    {
        Assert.DoesNotContain("Authentication error", result);
        Assert.DoesNotContain("authentication required", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Assert that the result is either valid JSON with expected properties or a descriptive message
    /// </summary>
    protected static void AssertValidResponseFormat(string result, string[] expectedJsonProperties, string[] expectedMessageTypes)
    {
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        try
        {
            var jsonDoc = JsonDocument.Parse(result);
            var root = jsonDoc.RootElement;

            // If it's JSON, verify it has at least one of the expected properties
            var hasExpectedProperty = expectedJsonProperties.Any(prop => root.TryGetProperty(prop, out _));
            Assert.True(hasExpectedProperty,
                $"JSON response should have one of these properties: {string.Join(", ", expectedJsonProperties)}");
        }
        catch (JsonException)
        {
            // If not JSON, should be a descriptive message
            var hasExpectedMessage = expectedMessageTypes.Any(msg => result.Contains(msg));
            Assert.True(hasExpectedMessage,
                $"Non-JSON response should contain one of: {string.Join(", ", expectedMessageTypes)}. Got: {result}");
        }
    }

    /// <summary>
    /// Skip test if the API response indicates upstream service blocking.
    /// Uses the actual response message as the skip reason.
    /// </summary>
    protected static void SkipIfUpstreamBlocked(string result)
    {
        Skip.If(result.Contains("Request is blocked by the upstream service"), result);
    }

    protected static void AssertResult(string scenarioType, string result)
    {

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Skip if upstream service is blocking requests
        SkipIfUpstreamBlocked(result);

        AssertNoAuthenticationError(result);

        if (result.Contains($"No {scenarioType} found"))
        {
            // If no connections, that's a valid response
            Assert.Contains($"No {scenarioType} found", result);
            return;
        }

        var jsonDoc = JsonDocument.Parse(result);

        // If it's JSON, verify it has the expected structure
        Assert.True(jsonDoc.RootElement.TryGetProperty("totalCount", out _), "JSON response should have totalCount property");
        Assert.True(jsonDoc.RootElement.TryGetProperty(scenarioType, out _), $"JSON response should have {scenarioType} property");
    }

    protected static void AssertOneItemResult(string scenarioType, string testId, string result)
    {
        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        AssertNoAuthenticationError(result);

        // Skip if upstream service is blocking requests
        SkipIfUpstreamBlocked(result);

        // The result should be either a "not found" message or a JSON object
        Assert.Equal($"{scenarioType} with ID '{testId}' not found or you don't have permission to access it.", result);
    }

}