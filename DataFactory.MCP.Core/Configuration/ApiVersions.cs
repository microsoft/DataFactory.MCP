namespace DataFactory.MCP.Configuration;

/// <summary>
/// Centralized API version management for all external APIs.
/// Keeping versions in one place makes it easier to update them when new API versions are released.
/// </summary>
public static class ApiVersions
{
    /// <summary>
    /// Azure Resource Manager API versions
    /// </summary>
    public static class AzureResourceManager
    {
        /// <summary>
        /// Subscriptions API version
        /// </summary>
        public const string Subscriptions = "2020-01-01";

        /// <summary>
        /// Resource Groups API version
        /// </summary>
        public const string ResourceGroups = "2021-04-01";

        /// <summary>
        /// Virtual Networks and Subnets API version
        /// </summary>
        public const string Network = "2023-04-01";
    }

    /// <summary>
    /// Microsoft Fabric API versions.
    /// The Fabric API uses path-based versioning (e.g., /v1/) rather than query parameters.
    /// </summary>
    public static class Fabric
    {
        /// <summary>
        /// Current Fabric API version (path-based: /v1/)
        /// </summary>
        public const string V1 = "v1";

        /// <summary>
        /// Full base URL for Fabric API v1
        /// </summary>
        public const string V1BaseUrl = "https://api.fabric.microsoft.com/v1";
    }

    /// <summary>
    /// Power BI API versions
    /// </summary>
    public static class PowerBi
    {
        /// <summary>
        /// Power BI API v2.0 (used for gateway cluster datasources)
        /// </summary>
        public const string V2 = "v2.0";

        /// <summary>
        /// Full base URL for Power BI API v2
        /// </summary>
        public const string V2BaseUrl = "https://api.powerbi.com/v2.0";
    }
}
