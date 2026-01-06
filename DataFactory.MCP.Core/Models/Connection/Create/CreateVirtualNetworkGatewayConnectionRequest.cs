using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection.Create;

/// <summary>
/// Request for creating virtual network gateway connections
/// </summary>
public class CreateVirtualNetworkGatewayConnectionRequest : CreateConnectionRequest
{
    [JsonPropertyName("gatewayId")]
    public string GatewayId { get; set; } = string.Empty;

    public CreateVirtualNetworkGatewayConnectionRequest()
    {
        ConnectivityType = ConnectivityType.VirtualNetworkGateway;
    }
}