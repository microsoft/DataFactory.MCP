using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Azure;

/// <summary>
/// Represents an Azure subscription
/// </summary>
public class AzureSubscription
{
    /// <summary>
    /// The subscription ID
    /// </summary>
    [JsonPropertyName("subscriptionId")]
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the subscription
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The state of the subscription
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// The tenant ID
    /// </summary>
    [JsonPropertyName("tenantId")]
    public string? TenantId { get; set; }
}

/// <summary>
/// Response for Azure subscriptions list API
/// </summary>
public class AzureSubscriptionsResponse
{
    /// <summary>
    /// The list of subscriptions
    /// </summary>
    [JsonPropertyName("value")]
    public List<AzureSubscription> Value { get; set; } = new();

    /// <summary>
    /// The URL for the next page of results
    /// </summary>
    [JsonPropertyName("nextLink")]
    public string? NextLink { get; set; }
}