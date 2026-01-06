using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection.Create;

/// <summary>
/// Request for creating cloud connections
/// </summary>
public class CreateCloudConnectionRequest : CreateConnectionRequest
{
    [JsonPropertyName("allowConnectionUsageInGateway")]
    public bool? AllowConnectionUsageInGateway { get; set; }

    public CreateCloudConnectionRequest()
    {
        ConnectivityType = ConnectivityType.ShareableCloud;
    }
}