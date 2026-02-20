/**
 * Pure helper functions for the CreateConnection wizard.
 * No side effects — each function derives data from its arguments.
 */

import {
  ConnectionMode,
  Gateway,
  SupportedDataSourceType,
} from "./services/types";

const gatewayTypeMap: Record<ConnectionMode, string> = {
  OnPremises: "OnPremises",
  VirtualNetwork: "VirtualNetwork",
  StreamingVirtualNetwork: "VirtualNetwork",
  Cloud: "",
};

/**
 * Returns gateways compatible with the given connection mode.
 * Returns an empty array for Cloud (no gateway needed).
 */
export function filterGatewaysByMode(
  gateways: Gateway[],
  connectionMode: ConnectionMode,
): Gateway[] {
  const targetType = gatewayTypeMap[connectionMode];
  if (!targetType) return [];
  return gateways.filter((gw) => gw.type === targetType);
}

/**
 * Finds the SupportedDataSourceType matching the selected display name.
 */
export function findSelectedType(
  types: SupportedDataSourceType[],
  displayName: string,
): SupportedDataSourceType | undefined {
  return types.find((t) => t.displayName === displayName);
}
