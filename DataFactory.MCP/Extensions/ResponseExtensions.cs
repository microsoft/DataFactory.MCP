using DataFactory.MCP.Models;
using DataFactory.MCP.Models.Connection;
using DataFactory.MCP.Models.Dataflow.Query;
using DataFactory.MCP.Models.Common.Responses.Errors;

namespace DataFactory.MCP.Extensions;

/// <summary>
/// Extension methods for creating standardized MCP tool responses
/// </summary>
public static class ResponseExtensions
{
    /// <summary>
    /// Creates an error response for failed query execution
    /// </summary>
    public static object ToQueryExecutionError(this ExecuteDataflowQueryResponse response, string workspaceId, string dataflowId, string queryName)
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
    /// Converts a Connection to a successful creation response with connection details
    /// </summary>
    public static object ToCreationSuccessResponse(this Connection connection, string message)
    {
        return new
        {
            Success = true,
            Message = message,
            Connection = new
            {
                Id = connection.Id,
                DisplayName = connection.DisplayName,
                ConnectivityType = connection.ConnectivityType.ToString(),
                ConnectionType = connection.ConnectionDetails.Type,
                Path = connection.ConnectionDetails.Path,
                PrivacyLevel = connection.PrivacyLevel?.ToString()
            }
        };
    }

    /// <summary>
    /// Converts an UnauthorizedAccessException to a standardized authentication error response
    /// </summary>
    public static McpAuthenticationErrorResponse ToAuthenticationError(this UnauthorizedAccessException ex)
    {
        return new McpAuthenticationErrorResponse(string.Format(Messages.AuthenticationErrorTemplate, ex.Message));
    }

    /// <summary>
    /// Converts an HttpRequestException to a standardized HTTP error response.
    /// Special handling for FabricApiException to provide more specific error messages.
    /// </summary>
    public static McpHttpErrorResponse ToHttpError(this HttpRequestException ex)
    {
        // Handle FabricApiException with more specific error information
        if (ex is FabricApiException fabricEx)
        {
            if (fabricEx.IsAuthenticationError)
            {
                return new McpHttpErrorResponse($"Authentication error: {Messages.AuthenticationRequired}");
            }

            if (fabricEx.IsRateLimited && fabricEx.RetryAfter.HasValue)
            {
                return new McpHttpErrorResponse($"Rate limited. Please retry after {fabricEx.RetryAfter.Value.TotalSeconds:F0} seconds.");
            }

            return new McpHttpErrorResponse(fabricEx.Message);
        }

        return new McpHttpErrorResponse(string.Format(Messages.ApiRequestFailedTemplate, ex.Message));
    }

    /// <summary>
    /// Converts an ArgumentException to a standardized validation error response
    /// </summary>
    public static McpValidationErrorResponse ToValidationError(this ArgumentException ex)
    {
        return new McpValidationErrorResponse(ex.Message);
    }

    /// <summary>
    /// Converts a general Exception to a standardized operation error response
    /// </summary>
    public static McpOperationErrorResponse ToOperationError(this Exception ex, string operation)
    {
        return new McpOperationErrorResponse(ex.Message, operation);
    }

    /// <summary>
    /// Creates a resource not found error response
    /// </summary>
    public static McpNotFoundErrorResponse ToNotFoundError(string resourceType, string resourceId)
    {
        return new McpNotFoundErrorResponse(resourceType, resourceId);
    }
}