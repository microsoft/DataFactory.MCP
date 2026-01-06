using DataFactory.MCP.Models.Connection;

namespace DataFactory.MCP.Extensions;

/// <summary>
/// Extension methods for Connection model transformations.
/// </summary>
public static class ConnectionExtensions
{

    /// <summary>
    /// Formats a Connection object for MCP API responses.
    /// Provides consistent output format and handles different connection types appropriately.
    /// </summary>
    /// <param name="connection">The connection object to format</param>
    /// <returns>Formatted object ready for JSON serialization</returns>
    public static object ToFormattedInfo(this Connection connection)
    {
        var baseInfo = new
        {
            Id = connection.Id,
            DisplayName = connection.DisplayName,
            ConnectivityType = connection.ConnectivityType.ToString(),
            ConnectionDetails = new
            {
                Type = connection.ConnectionDetails.Type,
                Path = connection.ConnectionDetails.Path
            },
            PrivacyLevel = connection.PrivacyLevel?.ToString(),
            CredentialDetails = connection.CredentialDetails != null ? new
            {
                CredentialType = connection.CredentialDetails.CredentialType.ToString(),
                SingleSignOnType = connection.CredentialDetails.SingleSignOnType.ToString(),
                ConnectionEncryption = connection.CredentialDetails.ConnectionEncryption.ToString(),
                SkipTestConnection = connection.CredentialDetails.SkipTestConnection
            } : null
        };

        return connection switch
        {
            ShareableCloudConnection shareable => new
            {
                baseInfo.Id,
                baseInfo.DisplayName,
                baseInfo.ConnectivityType,
                baseInfo.ConnectionDetails,
                baseInfo.PrivacyLevel,
                baseInfo.CredentialDetails,
                AllowConnectionUsageInGateway = shareable.AllowConnectionUsageInGateway
            },
            PersonalCloudConnection personal => new
            {
                baseInfo.Id,
                baseInfo.DisplayName,
                baseInfo.ConnectivityType,
                baseInfo.ConnectionDetails,
                baseInfo.PrivacyLevel,
                baseInfo.CredentialDetails,
                AllowConnectionUsageInGateway = personal.AllowConnectionUsageInGateway
            },
            OnPremisesGatewayConnection onPrem => new
            {
                baseInfo.Id,
                baseInfo.DisplayName,
                baseInfo.ConnectivityType,
                baseInfo.ConnectionDetails,
                baseInfo.PrivacyLevel,
                baseInfo.CredentialDetails,
                GatewayId = onPrem.GatewayId
            },
            OnPremisesGatewayPersonalConnection onPremPersonal => new
            {
                baseInfo.Id,
                baseInfo.DisplayName,
                baseInfo.ConnectivityType,
                baseInfo.ConnectionDetails,
                baseInfo.PrivacyLevel,
                baseInfo.CredentialDetails,
                GatewayId = onPremPersonal.GatewayId
            },
            VirtualNetworkGatewayConnection vnet => new
            {
                baseInfo.Id,
                baseInfo.DisplayName,
                baseInfo.ConnectivityType,
                baseInfo.ConnectionDetails,
                baseInfo.PrivacyLevel,
                baseInfo.CredentialDetails,
                GatewayId = vnet.GatewayId
            },
            // Return base info for unknown connection types
            _ => baseInfo
        };
    }
}