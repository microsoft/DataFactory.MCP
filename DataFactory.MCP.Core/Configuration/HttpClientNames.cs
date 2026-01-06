namespace DataFactory.MCP.Configuration;

/// <summary>
/// Constants for named HttpClient instances used throughout the application.
/// These names correspond to the HttpClient registrations in Program.cs.
/// </summary>
public static class HttpClientNames
{
    /// <summary>
    /// HttpClient for Microsoft Fabric API calls (api.fabric.microsoft.com)
    /// </summary>
    public const string FabricApi = "FabricApi";

    /// <summary>
    /// HttpClient for Azure Resource Manager API calls (management.azure.com)
    /// </summary>
    public const string AzureResourceManager = "AzureResourceManager";

    /// <summary>
    /// HttpClient for Power BI API calls (api.powerbi.com)
    /// </summary>
    public const string PowerBiV2Api = "PowerBiV2Api";
}
