using System.Text.Json;
using DataFactory.MCP.Models;

namespace DataFactory.MCP.Models.Connection.Formatters;

/// <summary>
/// Helper class for formatting Microsoft Fabric data source connection results consistently
/// </summary>
public static class FabricConnectionResultFormatter
{
    /// <summary>
    /// Formats a successful connection creation result
    /// </summary>
    public static string FormatConnectionResult(Connection connection, string message)
    {
        var result = new
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

        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Formats an error result based on exception type
    /// </summary>
    public static string FormatErrorResult(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException => string.Format(Messages.AuthenticationErrorTemplate, ex.Message),
            HttpRequestException => string.Format(Messages.ApiRequestFailedTemplate, ex.Message),
            ArgumentException => ex.Message, // Validation errors are already formatted
            _ => $"Error creating connection: {ex.Message}"
        };
    }
}