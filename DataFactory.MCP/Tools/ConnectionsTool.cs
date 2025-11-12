using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Models;
using DataFactory.MCP.Models.Connection.Factories;

namespace DataFactory.MCP.Tools;

[McpServerToolType]
public class ConnectionsTool
{
    private readonly IFabricConnectionService _connectionService;
    private readonly FabricDataSourceConnectionFactory _connectionFactory;
    private readonly IValidationService _validationService;

    public ConnectionsTool(
        IFabricConnectionService connectionService,
        FabricDataSourceConnectionFactory connectionFactory,
        IValidationService validationService)
    {
        _connectionService = connectionService;
        _connectionFactory = connectionFactory;
        _validationService = validationService;
    }

    [McpServerTool, Description(@"Lists all connections the user has permission for, including on-premises, virtual network and cloud connections")]
    public async Task<string> ListConnectionsAsync(
        [Description("A token for retrieving the next page of results (optional)")] string? continuationToken = null)
    {
        try
        {
            var response = await _connectionService.ListConnectionsAsync(continuationToken);

            if (!response.Value.Any())
            {
                return Messages.NoConnectionsFound;
            }

            var result = new
            {
                TotalCount = response.Value.Count,
                ContinuationToken = response.ContinuationToken,
                HasMoreResults = !string.IsNullOrEmpty(response.ContinuationToken),
                Connections = response.Value.Select(c => c.ToFormattedInfo())
            };

            return result.ToMcpJson();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ex.ToAuthenticationError().ToMcpJson();
        }
        catch (HttpRequestException ex)
        {
            return ex.ToHttpError().ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("listing connections").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Gets details about a specific connection by its ID")]
    public async Task<string> GetConnectionAsync(
        [Description("The ID of the connection to retrieve")] string connectionId)
    {
        try
        {
            _validationService.ValidateRequiredString(connectionId, nameof(connectionId));

            var connection = await _connectionService.GetConnectionAsync(connectionId);

            if (connection == null)
            {
                return ResponseExtensions.ToNotFoundError("Connection", connectionId).ToMcpJson();
            }

            var result = connection.ToFormattedInfo();
            return result.ToMcpJson();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ex.ToAuthenticationError().ToMcpJson();
        }
        catch (ArgumentException ex)
        {
            return ex.ToValidationError().ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("retrieving connection").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Creates a cloud SQL connection with basic authentication - simplified interface")]
    public async Task<string> CreateCloudSqlBasicAsync(
        [Description("The display name of the connection")] string displayName,
        [Description("The SQL Server name (e.g., server.database.windows.net)")] string serverName,
        [Description("The database name")] string databaseName,
        [Description("The username for authentication")] string username,
        [Description("The password for authentication")] string password)
    {
        try
        {
            var connection = await _connectionFactory.CreateCloudSqlBasicAsync(
                displayName, serverName, databaseName, username, password);

            return connection.ToCreationSuccessResponse("Cloud SQL connection with basic authentication created successfully").ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("creating cloud SQL connection").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Creates a cloud SQL connection with workspace identity authentication")]
    public async Task<string> CreateCloudSqlWorkspaceIdentityAsync(
        [Description("The display name of the connection")] string displayName,
        [Description("The SQL Server name (e.g., server.database.windows.net)")] string serverName,
        [Description("The database name")] string databaseName)
    {
        try
        {
            var connection = await _connectionFactory.CreateCloudSqlWorkspaceIdentityAsync(
                displayName, serverName, databaseName);

            return connection.ToCreationSuccessResponse("Cloud SQL connection with workspace identity created successfully").ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("creating cloud SQL workspace identity connection").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Creates a cloud web connection with anonymous authentication")]
    public async Task<string> CreateCloudWebAnonymousAsync(
        [Description("The display name of the connection")] string displayName,
        [Description("The web URL to connect to")] string url)
    {
        try
        {
            var connection = await _connectionFactory.CreateCloudWebAnonymousAsync(displayName, url);

            return connection.ToCreationSuccessResponse("Cloud web connection with anonymous authentication created successfully").ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("creating cloud web anonymous connection").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Creates a cloud web connection with basic authentication")]
    public async Task<string> CreateCloudWebBasicAsync(
        [Description("The display name of the connection")] string displayName,
        [Description("The web URL to connect to")] string url,
        [Description("The username for authentication")] string username,
        [Description("The password for authentication")] string password)
    {
        try
        {
            var connection = await _connectionFactory.CreateCloudWebBasicAsync(
                displayName, url, username, password);

            return connection.ToCreationSuccessResponse("Cloud web connection with basic authentication created successfully").ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("creating cloud web basic connection").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Creates a VNet gateway SQL connection with basic authentication")]
    public async Task<string> CreateVNetSqlBasicAsync(
        [Description("The display name of the connection")] string displayName,
        [Description("The virtual network gateway ID (UUID)")] string gatewayId,
        [Description("The SQL Server name (e.g., server.database.windows.net)")] string serverName,
        [Description("The database name")] string databaseName,
        [Description("The username for authentication")] string username,
        [Description("The password for authentication")] string password)
    {
        try
        {
            var connection = await _connectionFactory.CreateVNetSqlBasicAsync(
                displayName, gatewayId, serverName, databaseName, username, password);

            return connection.ToCreationSuccessResponse("VNet gateway SQL connection with basic authentication created successfully").ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("creating VNet gateway SQL connection").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Creates a VNet gateway SQL connection with workspace identity authentication")]
    public async Task<string> CreateVNetSqlWorkspaceIdentityAsync(
        [Description("The display name of the connection")] string displayName,
        [Description("The virtual network gateway ID (UUID)")] string gatewayId,
        [Description("The SQL Server name (e.g., server.database.windows.net)")] string serverName,
        [Description("The database name")] string databaseName)
    {
        try
        {
            var connection = await _connectionFactory.CreateVNetSqlWorkspaceIdentityAsync(
                displayName, gatewayId, serverName, databaseName);

            return connection.ToCreationSuccessResponse("VNet gateway SQL connection with workspace identity created successfully").ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("creating VNet gateway SQL workspace identity connection").ToMcpJson();
        }
    }
}
