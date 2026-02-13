using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Models;
using DataFactory.MCP.Models.Connection;

namespace DataFactory.MCP.Tools;

[McpServerToolType]
public class ConnectionsTool
{
    private readonly IFabricConnectionService _connectionService;
    private readonly IValidationService _validationService;

    public ConnectionsTool(
        IFabricConnectionService connectionService,
        IValidationService validationService)
    {
        _connectionService = connectionService;
        _validationService = validationService;
    }

    [McpServerTool, Description(@"Lists supported connection types with their creation methods, parameters, and supported credential types. Used to populate the Create Connection form.")]
    public async Task<string> ListSupportedConnectionTypesAsync(
        [Description("Optional gateway ID to filter supported types for a specific gateway")] string? gatewayId = null)
    {
        try
        {
            var response = await _connectionService.ListSupportedConnectionTypesAsync(gatewayId);

            if (!response.Value.Any())
            {
                return new { Error = "No supported connection types found." }.ToMcpJson();
            }

            // Log the first few types to verify casing from the API
            var sampleTypes = string.Join(", ", response.Value.Take(5).Select(ct => ct.Type));
            System.Console.Error.WriteLine($"[ListSupportedConnectionTypes] Sample types from API: {sampleTypes}");

            var result = new
            {
                TotalCount = response.Value.Count,
                ConnectionTypes = response.Value.Select(ct => new
                {
                    Type = ct.Type,
                    CreationMethods = ct.CreationMethods.Select(cm => new
                    {
                        Name = cm.Name,
                        Parameters = cm.Parameters.Select(p => new
                        {
                            Name = p.Name,
                            DataType = p.DataType,
                            Required = p.Required,
                            AllowedValues = p.AllowedValues
                        })
                    }),
                    SupportedCredentialTypes = ct.SupportedCredentialTypes,
                    SupportedConnectionEncryptionTypes = ct.SupportedConnectionEncryptionTypes,
                    SupportsSkipTestConnection = ct.SupportsSkipTestConnection
                })
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
            return ex.ToOperationError("listing supported connection types").ToMcpJson();
        }
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

    [McpServerTool, Description(@"Creates a new data source connection. Supports cloud, on-premises (gateway), and virtual network connectivity types.")]
    public async Task<string> CreateConnectionAsync(
        [Description("Display name for the connection")] string connectionName,
        [Description("Connection type identifier, e.g. 'SQL', 'Web', 'AzureBlobs', etc.")] string connectionType = "SQL",
        [Description("Creation method name from ListSupportedConnectionTypes (e.g. 'SQL', 'Web', 'AzureBlobs'). Defaults to connectionType if not specified.")] string? creationMethod = null,
        [Description("Connection parameters as JSON string of name:value pairs, e.g. '{\"server\":\"myserver.com\",\"database\":\"mydb\"}' ")] string? connectionParameters = null,
        [Description("Credential type: Anonymous, Basic, Windows, OAuth2, Key, SharedAccessSignature, ServicePrincipal, WorkspaceIdentity, KeyPair")] string credentialType = "Anonymous",
        [Description("Credentials as JSON name-value pairs (e.g., '{\"username\":\"user\",\"password\":\"pass\"}')")] string? credentials = null,
        [Description("Privacy level: None, Organizational, Public, or Private")] string privacyLevel = "None",
        [Description("Connection encryption: NotEncrypted, Encrypted, or Any")] string connectionEncryption = "NotEncrypted",
        [Description("Whether to skip test connection")] bool skipTestConnection = false,
        [Description("Connectivity type: ShareableCloud, OnPremisesGateway, or VirtualNetworkGateway")] string connectivityType = "ShareableCloud",
        [Description("Gateway ID (required for OnPremisesGateway and VirtualNetworkGateway types)")] string? gatewayId = null)
    {
        try
        {
            _validationService.ValidateRequiredString(connectionName, nameof(connectionName));
            System.Console.Error.WriteLine($"[CreateConnection] connectionType='{connectionType}', creationMethod='{creationMethod}'");

            // Validate gateway is provided for non-cloud types
            var needsGateway = connectivityType is "OnPremisesGateway" or "VirtualNetworkGateway";
            if (needsGateway && string.IsNullOrWhiteSpace(gatewayId))
            {
                return new { Error = "gatewayId is required for OnPremisesGateway and VirtualNetworkGateway connectivity types" }.ToMcpJson();
            }

            // Build connection parameters from JSON
            List<CreateConnectionParameter>? parameters = null;
            if (!string.IsNullOrWhiteSpace(connectionParameters))
            {
                var paramDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(connectionParameters);
                if (paramDict != null && paramDict.Count > 0)
                {
                    parameters = paramDict.Select(kvp => new CreateConnectionParameter
                    {
                        DataType = "Text",
                        Name = kvp.Key,
                        Value = kvp.Value
                    }).ToList();
                }
            }

            // Build credentials from JSON
            var creds = new CreateCredentials { CredentialType = credentialType };
            if (!string.IsNullOrWhiteSpace(credentials))
            {
                var credDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(credentials);
                if (credDict != null)
                {
                    // Map well-known credential fields
                    if (credDict.TryGetValue("username", out var username)) creds.Username = username;
                    if (credDict.TryGetValue("password", out var password)) creds.Password = password;
                    if (credDict.TryGetValue("key", out var key)) creds.Key = key;
                    if (credDict.TryGetValue("Token", out var token)) creds.Token = token;
                    if (credDict.TryGetValue("tenantId", out var tenantId)) creds.TenantId = tenantId;
                    if (credDict.TryGetValue("servicePrincipalClientId", out var spClientId)) creds.ServicePrincipalClientId = spClientId;
                    if (credDict.TryGetValue("servicePrincipalSecret", out var spSecret)) creds.ServicePrincipalSecret = spSecret;
                }
            }

            var request = new CreateConnectionRequest
            {
                ConnectivityType = connectivityType,
                DisplayName = connectionName,
                ConnectionDetails = new CreateConnectionDetails
                {
                    Type = connectionType,
                    CreationMethod = creationMethod ?? connectionType,
                    Parameters = parameters
                },
                CredentialDetails = new CreateCredentialDetails
                {
                    SingleSignOnType = "None",
                    ConnectionEncryption = connectionEncryption,
                    SkipTestConnection = skipTestConnection,
                    Credentials = creds
                },
                PrivacyLevel = privacyLevel,
                GatewayId = gatewayId
            };

            var connection = await _connectionService.CreateConnectionAsync(request);

            if (connection == null)
            {
                return new { Error = "Failed to create connection. The API returned no response." }.ToMcpJson();
            }

            var result = new
            {
                Success = true,
                ConnectionId = connection.Id,
                ConnectionName = connection.DisplayName,
                ConnectivityType = connection.ConnectivityType.ToString(),
                ConnectionDetails = new
                {
                    Type = connection.ConnectionDetails.Type,
                    Path = connection.ConnectionDetails.Path
                }
            };

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
        catch (HttpRequestException ex)
        {
            return ex.ToHttpError().ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("creating connection").ToMcpJson();
        }
    }
}
