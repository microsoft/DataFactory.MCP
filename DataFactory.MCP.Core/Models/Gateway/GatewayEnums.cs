namespace DataFactory.MCP.Models.Gateway;

/// <summary>
/// The type of the gateway
/// </summary>
public enum GatewayType
{
    /// <summary>
    /// The on-premises gateway
    /// </summary>
    OnPremises,

    /// <summary>
    /// The on-premises gateway (personal mode)
    /// </summary>
    OnPremisesPersonal,

    /// <summary>
    /// The virtual network gateway
    /// </summary>
    VirtualNetwork
}

/// <summary>
/// The load balancing setting of the gateway cluster
/// </summary>
public enum LoadBalancingSetting
{
    /// <summary>
    /// Requests will be sent to the first available gateway cluster member
    /// </summary>
    Failover,

    /// <summary>
    /// Requests will be distributed evenly among all enabled gateway cluster members
    /// </summary>
    DistributeEvenly
}
