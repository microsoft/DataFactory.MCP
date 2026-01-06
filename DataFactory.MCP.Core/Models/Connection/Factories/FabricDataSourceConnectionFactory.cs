using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models.Connection.Interfaces;
using DataFactory.MCP.Models.Connection.Configurations;
using DataFactory.MCP.Models.Connection.Validators;
using DataFactory.MCP.Models.Connection.Create;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Models.Connection.Factories;

/// <summary>
/// Factory for creating Microsoft Fabric data source connections with minimal code duplication
/// </summary>
public class FabricDataSourceConnectionFactory
{
    private readonly IFabricConnectionService _connectionService;
    private readonly IValidationService _validationService;
    private readonly ILogger<FabricDataSourceConnectionFactory> _logger;

    public FabricDataSourceConnectionFactory(
        IFabricConnectionService connectionService,
        IValidationService validationService,
        ILogger<FabricDataSourceConnectionFactory> logger)
    {
        _connectionService = connectionService;
        _validationService = validationService;
        _logger = logger;
    }

    /// <summary>
    /// Core method that handles all connection creation logic
    /// </summary>
    private async Task<T> CreateConnectionCoreAsync<T>(
        ConnectivityType connectivityType,
        IDataSourceConfig dataSourceConfig,
        IAuthConfig authConfig,
        ConnectionMetadata metadata,
        string? gatewayId = null) where T : Connection
    {
        try
        {
            var request = BuildConnectionRequest(connectivityType, dataSourceConfig, authConfig, metadata, gatewayId);

            return connectivityType switch
            {
                ConnectivityType.ShareableCloud => await _connectionService.CreateCloudConnectionAsync((CreateCloudConnectionRequest)request) as T,
                ConnectivityType.VirtualNetworkGateway => await _connectionService.CreateVirtualNetworkGatewayConnectionAsync((CreateVirtualNetworkGatewayConnectionRequest)request) as T,
                _ => throw new NotSupportedException($"Connectivity type {connectivityType} is not yet supported")
            } ?? throw new InvalidOperationException("Failed to create connection");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating {ConnectivityType} connection: {DisplayName}", connectivityType, metadata.DisplayName);
            throw;
        }
    }

    /// <summary>
    /// Builds the appropriate connection request based on connectivity type
    /// </summary>
    private CreateConnectionRequest BuildConnectionRequest(
        ConnectivityType connectivityType,
        IDataSourceConfig dataSourceConfig,
        IAuthConfig authConfig,
        ConnectionMetadata metadata,
        string? gatewayId)
    {
        var connectionDetails = new CreateConnectionDetails
        {
            Type = dataSourceConfig.Type,
            CreationMethod = dataSourceConfig.CreationMethod,
            Parameters = dataSourceConfig.GetParameters()
        };

        var credentialDetails = new CreateCredentialDetails
        {
            SingleSignOnType = authConfig.SingleSignOnType,
            ConnectionEncryption = authConfig.ConnectionEncryption,
            SkipTestConnection = authConfig.SkipTestConnection,
            Credentials = authConfig.GetCredentials()
        };

        return connectivityType switch
        {
            ConnectivityType.ShareableCloud => new CreateCloudConnectionRequest
            {
                DisplayName = metadata.DisplayName,
                PrivacyLevel = metadata.PrivacyLevel,
                AllowConnectionUsageInGateway = metadata.AllowConnectionUsageInGateway,
                ConnectionDetails = connectionDetails,
                CredentialDetails = credentialDetails
            },
            ConnectivityType.VirtualNetworkGateway => new CreateVirtualNetworkGatewayConnectionRequest
            {
                DisplayName = metadata.DisplayName,
                PrivacyLevel = metadata.PrivacyLevel,
                GatewayId = gatewayId ?? throw new ArgumentException("Gateway ID is required for VNet gateway connections"),
                ConnectionDetails = connectionDetails,
                CredentialDetails = credentialDetails
            },
            _ => throw new NotSupportedException($"Connectivity type {connectivityType} is not yet supported")
        };
    }

    // =============================================================================
    // CLOUD SQL SERVER CONNECTIONS
    // =============================================================================

    /// <summary>
    /// Creates a cloud SQL Server connection with basic authentication
    /// </summary>
    public async Task<Connection> CreateCloudSqlBasicAsync(
        string displayName,
        string serverName,
        string databaseName,
        string username,
        string password)
    {
        // Validate parameters using the validation service
        var validator = new FabricConnectionParameterValidator(_validationService);
        validator.ValidateSqlConnectionParameters(displayName, serverName, databaseName, username, password);

        return await CreateConnectionCoreAsync<Connection>(
            ConnectivityType.ShareableCloud,
            SqlDataSourceConfig.Create(serverName, databaseName),
            BasicAuthConfig.Create(username, password, skipTestConnection: false),
            ConnectionMetadata.Create(displayName, PrivacyLevel.Organizational)
        );
    }

    /// <summary>
    /// Creates a cloud SQL Server connection with workspace identity
    /// </summary>
    public async Task<Connection> CreateCloudSqlWorkspaceIdentityAsync(
        string displayName,
        string serverName,
        string databaseName)
    {
        // Validate parameters using the validation service  
        var validator = new FabricConnectionParameterValidator(_validationService);
        validator.ValidateSqlConnectionParameters(displayName, serverName, databaseName);

        return await CreateConnectionCoreAsync<Connection>(
            ConnectivityType.ShareableCloud,
            SqlDataSourceConfig.Create(serverName, databaseName),
            WorkspaceIdentityAuthConfig.Create(skipTestConnection: false),
            ConnectionMetadata.Create(displayName, PrivacyLevel.Organizational)
        );
    }

    // =============================================================================
    // CLOUD WEB CONNECTIONS
    // =============================================================================

    /// <summary>
    /// Creates a cloud web connection with anonymous authentication
    /// </summary>
    public async Task<Connection> CreateCloudWebAnonymousAsync(
        string displayName,
        string url)
    {
        // Validate parameters using the validation service
        var validator = new FabricConnectionParameterValidator(_validationService);
        validator.ValidateWebConnectionParameters(displayName, url);

        return await CreateConnectionCoreAsync<Connection>(
            ConnectivityType.ShareableCloud,
            WebDataSourceConfig.Create(url),
            AnonymousAuthConfig.Create(skipTestConnection: false),
            ConnectionMetadata.Create(displayName, PrivacyLevel.Organizational)
        );
    }

    /// <summary>
    /// Creates a cloud web connection with basic authentication
    /// </summary>
    public async Task<Connection> CreateCloudWebBasicAsync(
        string displayName,
        string url,
        string username,
        string password)
    {
        // Validate parameters using the validation service
        var validator = new FabricConnectionParameterValidator(_validationService);
        validator.ValidateWebConnectionParameters(displayName, url, username, password);

        return await CreateConnectionCoreAsync<Connection>(
            ConnectivityType.ShareableCloud,
            WebDataSourceConfig.Create(url),
            BasicAuthConfig.Create(username, password, skipTestConnection: false),
            ConnectionMetadata.Create(displayName, PrivacyLevel.Organizational)
        );
    }

    // =============================================================================
    // VIRTUAL NETWORK GATEWAY CONNECTIONS
    // =============================================================================

    /// <summary>
    /// Creates a VNet gateway SQL Server connection with basic authentication
    /// </summary>
    public async Task<Connection> CreateVNetSqlBasicAsync(
        string displayName,
        string gatewayId,
        string serverName,
        string databaseName,
        string username,
        string password)
    {
        // Validate parameters using the validation service
        var validator = new FabricConnectionParameterValidator(_validationService);
        validator.ValidateVNetGatewayConnectionParameters(displayName, gatewayId, serverName, databaseName, username, password);

        return await CreateConnectionCoreAsync<Connection>(
            ConnectivityType.VirtualNetworkGateway,
            SqlDataSourceConfig.Create(serverName, databaseName),
            BasicAuthConfig.Create(username, password, skipTestConnection: false),
            ConnectionMetadata.Create(displayName, PrivacyLevel.Organizational),
            gatewayId
        );
    }

    /// <summary>
    /// Creates a VNet gateway SQL Server connection with workspace identity
    /// </summary>
    public async Task<Connection> CreateVNetSqlWorkspaceIdentityAsync(
        string displayName,
        string gatewayId,
        string serverName,
        string databaseName)
    {
        // Validate parameters using the validation service
        var validator = new FabricConnectionParameterValidator(_validationService);
        validator.ValidateVNetGatewayConnectionParameters(displayName, gatewayId, serverName, databaseName);

        return await CreateConnectionCoreAsync<Connection>(
            ConnectivityType.VirtualNetworkGateway,
            SqlDataSourceConfig.Create(serverName, databaseName),
            WorkspaceIdentityAuthConfig.Create(skipTestConnection: false),
            ConnectionMetadata.Create(displayName, PrivacyLevel.Organizational),
            gatewayId
        );
    }
}
