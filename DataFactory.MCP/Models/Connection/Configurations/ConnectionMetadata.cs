namespace DataFactory.MCP.Models.Connection.Configurations;

/// <summary>
/// Metadata for connection creation
/// </summary>
public class ConnectionMetadata
{
    public string DisplayName { get; set; } = string.Empty;
    public PrivacyLevel PrivacyLevel { get; set; } = PrivacyLevel.Organizational;
    public bool AllowConnectionUsageInGateway { get; set; } = false;

    public static ConnectionMetadata Create(string displayName, PrivacyLevel privacyLevel = PrivacyLevel.Organizational)
    {
        return new ConnectionMetadata
        {
            DisplayName = displayName,
            PrivacyLevel = privacyLevel
        };
    }
}