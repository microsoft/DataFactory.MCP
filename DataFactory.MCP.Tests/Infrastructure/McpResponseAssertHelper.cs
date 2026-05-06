using System.Text.Json;
using Fabric.Mcp.Tools.DataFactory.Models.Common.Responses;
using Fabric.Mcp.Tools.DataFactory.Models.Common.Responses.Errors;
using Xunit;

namespace DataFactory.MCP.Tests.Infrastructure;

/// <summary>
/// Helper class for asserting MCP responses in tests
/// </summary>
public static class McpResponseAssertHelper
{
    /// <summary>
    /// Standard JSON serialization options for MCP tool responses (matching JsonExtensions)
    /// </summary>
    private static readonly JsonSerializerOptions McpJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Asserts that the result is a valid validation error response
    /// </summary>
    public static void AssertValidationError(string jsonResult, string? expectedMessageContains = null)
    {
        Assert.NotNull(jsonResult);
        Assert.True(IsValidJson(jsonResult), "Result should be valid JSON");

        var response = JsonSerializer.Deserialize<McpValidationErrorResponse>(jsonResult, McpJsonOptions);
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("ValidationError", response.Error);
        Assert.NotNull(response.Message);

        if (expectedMessageContains != null)
        {
            Assert.Contains(expectedMessageContains, response.Message);
        }
    }

    /// <summary>
    /// Asserts that the result is a valid authentication error response
    /// </summary>
    public static void AssertAuthenticationError(string jsonResult)
    {
        Assert.NotNull(jsonResult);
        Assert.True(IsValidJson(jsonResult), "Result should be valid JSON");

        var response = JsonSerializer.Deserialize<McpAuthenticationErrorResponse>(jsonResult, McpJsonOptions);
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("AuthenticationError", response.Error);
        Assert.NotNull(response.Message);
    }

    /// <summary>
    /// Asserts that the result is a valid HTTP request error response
    /// </summary>
    public static void AssertHttpError(string jsonResult, string? expectedMessageContains = null)
    {
        Assert.NotNull(jsonResult);
        Assert.True(IsValidJson(jsonResult), "Result should be valid JSON");

        var response = JsonSerializer.Deserialize<McpHttpErrorResponse>(jsonResult, McpJsonOptions);
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("HttpRequestError", response.Error);
        Assert.NotNull(response.Message);

        if (expectedMessageContains != null)
        {
            Assert.Contains(expectedMessageContains, response.Message);
        }
    }

    /// <summary>
    /// Asserts that the result is a valid operation error response
    /// </summary>
    public static void AssertOperationError(string jsonResult, string expectedOperation)
    {
        Assert.NotNull(jsonResult);
        Assert.True(IsValidJson(jsonResult), "Result should be valid JSON");

        var response = JsonSerializer.Deserialize<McpOperationErrorResponse>(jsonResult, McpJsonOptions);
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("OperationError", response.Error);
        Assert.Equal(expectedOperation, response.Operation);
        Assert.NotNull(response.Message);
    }

    /// <summary>
    /// Asserts that the result is a valid not found error response
    /// </summary>
    public static void AssertNotFoundError(string jsonResult, string expectedResourceType, string expectedResourceId)
    {
        Assert.NotNull(jsonResult);
        Assert.True(IsValidJson(jsonResult), "Result should be valid JSON");

        var response = JsonSerializer.Deserialize<McpNotFoundErrorResponse>(jsonResult, McpJsonOptions);
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Equal("NotFoundError", response.Error);
        Assert.Equal(expectedResourceType, response.ResourceType);
        Assert.Equal(expectedResourceId, response.ResourceId);
        Assert.NotNull(response.Message);
    }

    /// <summary>
    /// Asserts that the result is a successful response
    /// </summary>
    public static T AssertSuccessResponse<T>(string jsonResult)
    {
        Assert.NotNull(jsonResult);
        Assert.True(IsValidJson(jsonResult), "Result should be valid JSON");

        var response = JsonSerializer.Deserialize<McpSuccessResponse<T>>(jsonResult, McpJsonOptions);
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);

        return response.Data;
    }

    /// <summary>
    /// Checks if a string is valid JSON
    /// </summary>
    public static bool IsValidJson(string jsonString)
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
    /// Generic method to deserialize and assert any error response type
    /// </summary>
    public static T AssertErrorResponse<T>(string jsonResult) where T : McpErrorResponse
    {
        Assert.NotNull(jsonResult);
        Assert.True(IsValidJson(jsonResult), "Result should be valid JSON");

        var response = JsonSerializer.Deserialize<T>(jsonResult);
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.NotNull(response.Message);

        return response;
    }
}