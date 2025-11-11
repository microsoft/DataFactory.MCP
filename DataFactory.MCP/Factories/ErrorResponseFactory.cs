using DataFactory.MCP.Models;
using DataFactory.MCP.Models.Dataflow;

namespace DataFactory.MCP.Factories;

/// <summary>
/// Factory class for creating standardized error responses
/// </summary>
public static class ErrorResponseFactory
{
    /// <summary>
    /// Creates an error response for failed query execution
    /// </summary>
    public static object CreateQueryExecutionError(ExecuteDataflowQueryResponse response, string workspaceId, string dataflowId, string queryName)
    {
        return new
        {
            Success = false,
            Error = response.Error,
            Message = $"Failed to execute query '{queryName}' on dataflow {dataflowId}",
            WorkspaceId = workspaceId,
            DataflowId = dataflowId,
            QueryName = queryName
        };
    }

    /// <summary>
    /// Creates a validation error response
    /// </summary>
    public static object CreateValidationError(string message)
    {
        return new
        {
            Success = false,
            Error = "ValidationError",
            Message = $"Validation failed: {message}"
        };
    }

    /// <summary>
    /// Creates an authentication error response
    /// </summary>
    public static object CreateAuthenticationError(string message)
    {
        return new
        {
            Success = false,
            Error = "AuthenticationError",
            Message = string.Format(Messages.AuthenticationErrorTemplate, message)
        };
    }

    /// <summary>
    /// Creates an HTTP error response
    /// </summary>
    public static object CreateHttpError(string message)
    {
        return new
        {
            Success = false,
            Error = "HttpRequestError",
            Message = string.Format(Messages.ApiRequestFailedTemplate, message)
        };
    }

    /// <summary>
    /// Creates a generic error response
    /// </summary>
    public static object CreateGenericError(string message)
    {
        return new
        {
            Success = false,
            Error = "ExecutionError",
            Message = $"Error executing dataflow query: {message}"
        };
    }
}