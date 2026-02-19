/**
 * Data fetching helpers for the Create Connection wizard.
 * Pure async functions — no React state, no side effects beyond the API calls.
 */

import {
  Gateway,
  SupportedConnectionType,
  SupportedDataSourceType,
} from "./types";
import { convertConnectionTypes } from "./convertConnectionTypes";

/** Matches the callServerTool signature on McpAppComponent */
export type CallServerTool = (
  tool: string,
  args: Record<string, unknown>,
) => Promise<unknown>;

// =============================================================================
// Gateways
// =============================================================================

export async function fetchGateways(
  callServerTool: CallServerTool,
): Promise<Gateway[]> {
  const result = await callServerTool("list_gateways", {});

  let parsed: {
    gateways?: Array<{
      id: string;
      displayName?: string;
      name?: string;
      type: string;
    }>;
  } | null = null;

  if (typeof result === "string") {
    try {
      parsed = JSON.parse(result);
    } catch {
      /* not JSON */
    }
  } else if (result && typeof result === "object") {
    parsed = result as typeof parsed;
  }

  return (parsed?.gateways || []).map((g) => ({
    id: g.id,
    name: g.displayName || g.name || g.id,
    type: g.type as Gateway["type"],
  }));
}

// =============================================================================
// Supported connection types
// =============================================================================

export async function fetchSupportedConnectionTypes(
  callServerTool: CallServerTool,
): Promise<SupportedDataSourceType[]> {
  const result = await callServerTool("list_supported_connection_types", {});

  let parsed: { connectionTypes?: SupportedConnectionType[] } | null = null;

  if (typeof result === "string") {
    try {
      parsed = JSON.parse(result);
    } catch {
      /* not JSON */
    }
  } else if (result && typeof result === "object") {
    parsed = result as typeof parsed;
  }

  const apiTypes: SupportedConnectionType[] = parsed?.connectionTypes || [];
  const supportedDataSourceTypes = convertConnectionTypes(apiTypes);

  console.log(
    `[CreateConnection] Loaded ${supportedDataSourceTypes.length} connection types from ${apiTypes.length} API types`,
  );

  return supportedDataSourceTypes;
}
