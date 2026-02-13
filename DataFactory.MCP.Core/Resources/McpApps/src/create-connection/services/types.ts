/**
 * TypeScript types for Create Connection MCP App
 * Matches PowerBI v2 API contracts for cloud data source creation.
 */

// =============================================================================
// Connection Modes
// =============================================================================

export type ConnectionMode =
  | "OnPremises"
  | "VirtualNetwork"
  | "StreamingVirtualNetwork"
  | "Cloud";

export type GatewayType =
  | "OnPremDataGateway"
  | "VirtualNetwork"
  | "StreamingVirtualNetwork"
  | "TenantCloud";

// =============================================================================
// Gateway Types
// =============================================================================

export interface Gateway {
  id: string;
  name: string;
  type: GatewayType;
}

// =============================================================================
// Supported Connection Types (from list_supported_connection_types tool)
// =============================================================================

/** A parameter within a creation method */
export interface ConnectionCreationParameter {
  name: string;
  dataType: string;
  required: boolean;
  allowedValues?: string[] | null;
}

/** A creation method for a connection type */
export interface ConnectionCreationMethod {
  name: string;
  parameters: ConnectionCreationParameter[];
}

/** Metadata for a supported connection type from the Fabric API */
export interface SupportedConnectionType {
  type: string;
  creationMethods: ConnectionCreationMethod[];
  supportedCredentialTypes: string[];
  supportedConnectionEncryptionTypes: string[];
  supportsSkipTestConnection: boolean;
}

/**
 * A label/field for connection detail parameters (used in the form UI).
 */
export interface DataSourceLabel {
  name: string;
  dataType: string;
  required: boolean;
  allowedValues?: string[] | null;
}

/**
 * UI-friendly data source type derived from SupportedConnectionType.
 * Each entry represents one (type, creationMethod) combination.
 */
export interface SupportedDataSourceType {
  /** The connection type (e.g. "SQL") */
  dataSourceType: string;
  /** The creation method name (e.g. "SQL") */
  creationMethod: string;
  /** Display label: "type" or "type (creationMethod)" when there are multiple */
  displayName: string;
  /** Parameters required by this creation method */
  labels: DataSourceLabel[];
  credentialTypes: string[];
  supportedEncryptionTypes: string[];
  isSkipTestConnectionSupported: boolean;
}

// =============================================================================
// Credential Types (values match Fabric API CredentialType enum)
// =============================================================================

export type CredentialType =
  | "Anonymous"
  | "Basic"
  | "Windows"
  | "WindowsWithoutImpersonation"
  | "OAuth2"
  | "Key"
  | "SharedAccessSignature"
  | "ServicePrincipal"
  | "WorkspaceIdentity"
  | "KeyPair";

/** Maps credential type to the fields it requires */
export const credentialFieldsMap: Record<
  string,
  Array<{ name: string; label: string; isSecret: boolean }>
> = {
  Anonymous: [],
  Basic: [
    { name: "username", label: "Username", isSecret: false },
    { name: "password", label: "Password", isSecret: true },
  ],
  Windows: [
    { name: "username", label: "Username", isSecret: false },
    { name: "password", label: "Password", isSecret: true },
  ],
  WindowsWithoutImpersonation: [],
  OAuth2: [],
  Key: [{ name: "key", label: "Account Key", isSecret: true }],
  SharedAccessSignature: [
    { name: "token", label: "SAS Token", isSecret: true },
  ],
  ServicePrincipal: [
    { name: "tenantId", label: "Tenant ID", isSecret: false },
    {
      name: "servicePrincipalClientId",
      label: "Service Principal Client ID",
      isSecret: false,
    },
    {
      name: "servicePrincipalSecret",
      label: "Service Principal Secret",
      isSecret: true,
    },
  ],
  WorkspaceIdentity: [],
  KeyPair: [
    { name: "identifier", label: "Identifier", isSecret: false },
    { name: "privateKey", label: "Private Key (PKCS #8)", isSecret: true },
    { name: "passphrase", label: "Passphrase (optional)", isSecret: true },
  ],
};

// =============================================================================
// Privacy Level
// =============================================================================

export type PrivacyLevel = "None" | "Private" | "Organizational" | "Public";

export const privacyLevelOptions: Array<{
  value: PrivacyLevel;
  label: string;
}> = [
  { value: "None", label: "None" },
  { value: "Private", label: "Private" },
  { value: "Organizational", label: "Organizational" },
  { value: "Public", label: "Public" },
];

// =============================================================================
// Create Connection (Fabric API tool args)
// =============================================================================

export interface CreateConnectionResult {
  success?: boolean;
  connectionId?: string;
  connectionName?: string;
  connectivityType?: string;
  connectionDetails?: {
    type?: string;
    path?: string;
  };
  error?: string;
  message?: string;
}

// =============================================================================
// UI State Types
// =============================================================================

export interface ToolResultData {
  accessToken?: string | null;
}

// =============================================================================
// Mapping Helpers
// =============================================================================

export const connectionModeLabels: Record<ConnectionMode, string> = {
  OnPremises: "On-premises",
  VirtualNetwork: "Virtual network",
  StreamingVirtualNetwork: "Streaming virtual network",
  Cloud: "Cloud",
};

export const connectionModeIcons: Record<ConnectionMode, string> = {
  OnPremises: "☁️↔️",
  VirtualNetwork: "🔗",
  StreamingVirtualNetwork: "📡",
  Cloud: "☁️",
};

export function requiresGateway(mode: ConnectionMode): boolean {
  return mode !== "Cloud";
}
