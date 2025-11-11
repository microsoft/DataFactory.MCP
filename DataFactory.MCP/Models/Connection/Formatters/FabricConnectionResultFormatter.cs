using System.Text.Json;
using DataFactory.MCP.Extensions;
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

        return result.ToMcpJson();
    }


}