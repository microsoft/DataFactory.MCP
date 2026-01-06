namespace DataFactory.MCP.Abstractions.Interfaces.DMTSv2;

/// <summary>
/// Service for retrieving ClusterId from the Power BI v2.0 API for cloud datasources.
/// The ClusterId is required for proper credential binding when adding connections to dataflows.
/// 
/// API Endpoint: GET https://api.powerbi.com/v2.0/myorg/me/gatewayClusterDatasources
/// </summary>
public interface IGatewayClusterDatasourceService
{
    /// <summary>
    /// Gets the ClusterId for a specific connectionId (cloud datasource ID).
    /// This is required to properly bind credentials when adding a connection to a dataflow.
    /// </summary>
    /// <param name="connectionId">The connection ID (datasource ID) to look up</param>
    /// <returns>The ClusterId if found, null otherwise</returns>
    Task<string?> GetClusterIdForConnectionAsync(string connectionId);
}
