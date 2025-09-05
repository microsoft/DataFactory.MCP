using DataFactory.MCP.Models.Gateway;

namespace DataFactory.MCP.Extensions;

/// <summary>
/// Extension methods for Gateway model transformations.
/// </summary>
public static class GatewayExtensions
{
    /// <summary>
    /// Formats a Gateway object for MCP API responses.
    /// Provides consistent output format, truncates sensitive data (public keys), 
    /// and handles different gateway types appropriately.
    /// </summary>
    /// <param name="gateway">The gateway object to format</param>
    /// <returns>Formatted object ready for JSON serialization</returns>
    public static object ToFormattedInfo(this Gateway gateway)
    {
        var baseInfo = new
        {
            Id = gateway.Id,
            Type = gateway.Type
        };

        return gateway switch
        {
            OnPremisesGateway onPrem => new
            {
                baseInfo.Id,
                baseInfo.Type,
                DisplayName = onPrem.DisplayName,
                Version = onPrem.Version,
                NumberOfMembers = onPrem.NumberOfMemberGateways,
                LoadBalancing = onPrem.LoadBalancingSetting,
                AllowCloudRefresh = onPrem.AllowCloudConnectionRefresh,
                AllowCustomConnectors = onPrem.AllowCustomConnectors,
                PublicKey = new
                {
                    Exponent = onPrem.PublicKey.Exponent,
                    // Truncate sensitive cryptographic data for security
                    Modulus = onPrem.PublicKey.Modulus.Length > 20
                        ? onPrem.PublicKey.Modulus[..20] + "..."
                        : onPrem.PublicKey.Modulus
                }
            },
            OnPremisesGatewayPersonal personal => new
            {
                baseInfo.Id,
                baseInfo.Type,
                Version = personal.Version,
                PublicKey = new
                {
                    Exponent = personal.PublicKey.Exponent,
                    // Truncate sensitive cryptographic data for security
                    Modulus = personal.PublicKey.Modulus.Length > 20
                        ? personal.PublicKey.Modulus[..20] + "..."
                        : personal.PublicKey.Modulus
                }
            },
            VirtualNetworkGateway vnet => new
            {
                baseInfo.Id,
                baseInfo.Type,
                DisplayName = vnet.DisplayName,
                CapacityId = vnet.CapacityId,
                NumberOfMembers = vnet.NumberOfMemberGateways,
                InactivityMinutes = vnet.InactivityMinutesBeforeSleep,
                VirtualNetwork = new
                {
                    SubscriptionId = vnet.VirtualNetworkAzureResource.SubscriptionId,
                    ResourceGroup = vnet.VirtualNetworkAzureResource.ResourceGroupName,
                    VNetName = vnet.VirtualNetworkAzureResource.VirtualNetworkName,
                    Subnet = vnet.VirtualNetworkAzureResource.SubnetName
                }
            },
            // Return minimal info for unknown gateway types
            _ => baseInfo
        };
    }
}
