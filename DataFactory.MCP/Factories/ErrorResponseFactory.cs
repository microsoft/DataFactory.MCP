using DataFactory.MCP.Models;
using DataFactory.MCP.Models.Dataflow;

namespace DataFactory.MCP.Factories;

/// <summary>
/// Factory class for creating standardized error responses across all MCP tools
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

    /// <summary>
    /// Creates a generic operation error response
    /// </summary>
    public static object CreateOperationError(string operation, string message)
    {
        return new
        {
            Success = false,
            Error = "OperationError",
            Message = $"Error {operation}: {message}",
            Operation = operation
        };
    }

    /// <summary>
    /// Creates a resource not found error response
    /// </summary>
    public static object CreateNotFoundError(string resourceType, string resourceId)
    {
        return new
        {
            Success = false,
            Error = "NotFoundError",
            Message = $"{resourceType} with ID '{resourceId}' was not found",
            ResourceType = resourceType,
            ResourceId = resourceId
        };
    }

    /// <summary>
    /// Creates a forbidden access error response
    /// </summary>
    public static object CreateForbiddenError(string message)
    {
        return new
        {
            Success = false,
            Error = "ForbiddenError",
            Message = $"Access denied: {message}"
        };
    }

    /// <summary>
    /// Creates a connection operation error response based on exception type
    /// </summary>
    public static object CreateConnectionError(Exception ex, string operation)
    {
        return ex switch
        {
            UnauthorizedAccessException => CreateAuthenticationError(ex.Message),
            HttpRequestException => CreateHttpError(ex.Message),
            ArgumentException => CreateValidationError(ex.Message),
            _ => CreateOperationError(operation, ex.Message)
        };
    }
}