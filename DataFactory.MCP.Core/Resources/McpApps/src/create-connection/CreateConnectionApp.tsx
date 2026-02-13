/**
 * CreateConnectionApp - Main Application Component
 *
 * Orchestrates the full cloud connection creation flow following the GAP pattern:
 * 1. Receives access token from MCP tool result
 * 2. Fetches supported data source types (connection type dropdown)
 * 3. Renders dynamic connection detail fields per selected type
 * 4. Renders credential section (auth method, credential fields, privacy level)
 * 5. Creates cloud data source via MCP server tool
 * 6. Shows success banner on completion
 *
 * All components are React class components.
 */

import { ReactNode } from "react";
import {
  McpAppComponent,
  McpAppComponentProps,
  McpAppComponentState,
  baseStyles,
  applyBodyStyles,
} from "../shared";
import {
  ConnectionMode,
  CredentialType,
  PrivacyLevel,
  SupportedDataSourceType,
  SupportedConnectionType,
  CreateConnectionResult,
  Gateway,
  requiresGateway,
  credentialFieldsMap,
} from "./services/types";
import { convertConnectionTypes } from "./services/convertConnectionTypes";
import {
  ConnectionModeSelector,
  GatewayDropdown,
  ConnectionNameInput,
  ConnectionTypeDropdown,
  ConnectionDetailFields,
  CredentialSection,
  FormButtons,
  SuccessBanner,
  ErrorBanner,
  LoadingSpinner,
} from "./components";

// =============================================================================
// State
// =============================================================================

interface CreateConnectionAppState extends McpAppComponentState {
  // Form data - basic
  connectionMode: ConnectionMode;
  connectionName: string;
  selectedGatewayId: string | null;

  // Form data - data source type
  selectedDataSourceType: string;
  connectionDetailValues: Record<string, string>;

  // Form data - credentials
  selectedCredentialType: CredentialType;
  credentialValues: Record<string, string>;
  privacyLevel: PrivacyLevel;
  encryptedConnection: string;
  skipTestConnection: boolean;

  // Loaded data
  gateways: Gateway[];
  isLoadingGateways: boolean;
  gatewayError: string | null;
  supportedDataSourceTypes: SupportedDataSourceType[];
  isLoadingConnectionTypes: boolean;
  connectionTypesError: string | null;

  // Submission
  isSubmitting: boolean;
  submitError: string | null;

  // Success
  createdConnectionId: string | null;
  createdConnectionName: string | null;
}

// =============================================================================
// Component
// =============================================================================

export class CreateConnectionApp extends McpAppComponent<
  McpAppComponentProps,
  CreateConnectionAppState
> {
  constructor(props: McpAppComponentProps) {
    super(props);

    this.state = {
      ...this.state,

      connectionMode: "Cloud",
      connectionName: "",
      selectedGatewayId: null,

      selectedDataSourceType: "",
      connectionDetailValues: {},

      selectedCredentialType: "Anonymous",
      credentialValues: {},
      privacyLevel: "None",
      encryptedConnection: "NotEncrypted",
      skipTestConnection: false,

      gateways: [],
      isLoadingGateways: false,
      gatewayError: null,
      supportedDataSourceTypes: [],
      isLoadingConnectionTypes: false,
      connectionTypesError: null,

      isSubmitting: false,
      submitError: null,

      createdConnectionId: null,
      createdConnectionName: null,
    };

    this.handleModeChange = this.handleModeChange.bind(this);
    this.handleGatewaySelect = this.handleGatewaySelect.bind(this);
    this.handleNameChange = this.handleNameChange.bind(this);
    this.handleDataSourceTypeChange =
      this.handleDataSourceTypeChange.bind(this);
    this.handleConnectionDetailChange =
      this.handleConnectionDetailChange.bind(this);
    this.handleCredentialTypeChange =
      this.handleCredentialTypeChange.bind(this);
    this.handleCredentialValueChange =
      this.handleCredentialValueChange.bind(this);
    this.handlePrivacyLevelChange = this.handlePrivacyLevelChange.bind(this);
    this.handleEncryptedConnectionChange =
      this.handleEncryptedConnectionChange.bind(this);
    this.handleSkipTestConnectionChange =
      this.handleSkipTestConnectionChange.bind(this);
    this.handleSubmit = this.handleSubmit.bind(this);
    this.handleCancel = this.handleCancel.bind(this);
    this.handleRetry = this.handleRetry.bind(this);
  }

  componentDidMount(): void {
    super.componentDidMount();
    applyBodyStyles();
  }

  // ===========================================================================
  // MCP Lifecycle
  // ===========================================================================

  protected onToolResult(_result: unknown): void {
    // No token needed — all API calls happen server-side via callServerTool.
    // FabricAuthenticationHandler handles auth automatically.
    // Fetch gateways and supported connection types in parallel.
    console.log("[CreateConnection] Connected, fetching data...");
    this.fetchGateways();
    this.fetchSupportedConnectionTypes();
  }

  // ===========================================================================
  // Data Fetching
  // ===========================================================================

  private async fetchGateways(): Promise<void> {
    this.setState({ isLoadingGateways: true, gatewayError: null });

    try {
      const result = await this.callServerTool("list_gateways", {});
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

      const gateways: Gateway[] = (parsed?.gateways || []).map((g) => ({
        id: g.id,
        name: g.displayName || g.name || g.id,
        type: g.type as Gateway["type"],
      }));

      this.setState({ gateways, isLoadingGateways: false });
    } catch (error) {
      this.setState({
        gatewayError:
          error instanceof Error ? error.message : "Failed to load gateways",
        isLoadingGateways: false,
      });
    }
  }

  private async fetchSupportedConnectionTypes(): Promise<void> {
    this.setState({
      isLoadingConnectionTypes: true,
      connectionTypesError: null,
    });

    try {
      const result = await this.callServerTool(
        "list_supported_connection_types",
        {},
      );

      let parsed: {
        connectionTypes?: SupportedConnectionType[];
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

      const apiTypes: SupportedConnectionType[] = parsed?.connectionTypes || [];

      const supportedDataSourceTypes = convertConnectionTypes(apiTypes);

      console.log(
        `[CreateConnection] Loaded ${supportedDataSourceTypes.length} connection types from ${apiTypes.length} API types`,
      );

      this.setState({
        supportedDataSourceTypes,
        isLoadingConnectionTypes: false,
      });
    } catch (error) {
      console.error(
        "[CreateConnection] Error loading connection types:",
        error,
      );
      this.setState({
        connectionTypesError:
          error instanceof Error
            ? error.message
            : "Failed to load connection types",
        isLoadingConnectionTypes: false,
      });
    }
  }

  // ===========================================================================
  // Event Handlers
  // ===========================================================================

  private handleModeChange(mode: ConnectionMode): void {
    this.setState({
      connectionMode: mode,
      selectedGatewayId: null,
      submitError: null,
    });
  }

  private handleGatewaySelect(gatewayId: string): void {
    this.setState({ selectedGatewayId: gatewayId, submitError: null });
  }

  private handleNameChange(value: string): void {
    this.setState({ connectionName: value, submitError: null });
  }

  private handleDataSourceTypeChange(dsType: string): void {
    // dsType is the displayName which is unique per (type, creationMethod) pair
    const selectedType = this.state.supportedDataSourceTypes.find(
      (t) => t.displayName === dsType,
    );

    // Pick first available credential type, or default to Anonymous
    const firstCredType =
      (selectedType?.credentialTypes?.[0] as CredentialType) || "Anonymous";

    this.setState({
      selectedDataSourceType: dsType,
      connectionDetailValues: {},
      selectedCredentialType: firstCredType,
      credentialValues: {},
      submitError: null,
    });
  }

  private handleConnectionDetailChange(fieldName: string, value: string): void {
    this.setState((prev) => ({
      connectionDetailValues: {
        ...prev.connectionDetailValues,
        [fieldName]: value,
      },
      submitError: null,
    }));
  }

  private handleCredentialTypeChange(type: CredentialType): void {
    this.setState({
      selectedCredentialType: type,
      credentialValues: {},
      submitError: null,
    });
  }

  private handleCredentialValueChange(fieldName: string, value: string): void {
    this.setState((prev) => ({
      credentialValues: {
        ...prev.credentialValues,
        [fieldName]: value,
      },
      submitError: null,
    }));
  }

  private handlePrivacyLevelChange(level: PrivacyLevel): void {
    this.setState({ privacyLevel: level });
  }

  private handleEncryptedConnectionChange(value: string): void {
    this.setState({ encryptedConnection: value });
  }

  private handleSkipTestConnectionChange(value: boolean): void {
    this.setState({ skipTestConnection: value });
  }

  private handleCancel(): void {
    this.setState({
      connectionName: "",
      selectedDataSourceType: "",
      connectionDetailValues: {},
      selectedCredentialType: "Anonymous",
      credentialValues: {},
      privacyLevel: "None",
      encryptedConnection: "NotEncrypted",
      skipTestConnection: false,
      selectedGatewayId: null,
      submitError: null,
    });
  }

  private handleRetry(): void {
    this.setState({
      submitError: null,
      gatewayError: null,
      connectionTypesError: null,
    });
    this.fetchGateways();
    this.fetchSupportedConnectionTypes();
  }

  // ===========================================================================
  // Submit
  // ===========================================================================

  private async handleSubmit(): Promise<void> {
    const {
      connectionName,
      connectionMode,
      selectedDataSourceType,
      connectionDetailValues,
      selectedCredentialType,
      credentialValues,
      privacyLevel,
      encryptedConnection,
      skipTestConnection,
      selectedGatewayId,
    } = this.state;

    // Validation
    if (!connectionName.trim()) {
      this.setState({ submitError: "Connection name is required" });
      return;
    }
    if (!selectedDataSourceType) {
      this.setState({ submitError: "Please select a connection type" });
      return;
    }
    if (requiresGateway(connectionMode) && !selectedGatewayId) {
      this.setState({ submitError: "Please select a gateway cluster" });
      return;
    }

    // Validate required connection detail fields
    const selectedType = this.state.supportedDataSourceTypes.find(
      (t) => t.displayName === selectedDataSourceType,
    );
    if (selectedType) {
      for (const label of selectedType.labels) {
        if (label.required && !connectionDetailValues[label.name]?.trim()) {
          this.setState({ submitError: `${label.name} is required` });
          return;
        }
      }
    }

    // Validate required credential fields
    const credFields = credentialFieldsMap[selectedCredentialType] || [];
    for (const field of credFields) {
      if (!credentialValues[field.name]?.trim()) {
        this.setState({ submitError: `${field.label} is required` });
        return;
      }
    }

    this.setState({ isSubmitting: true, submitError: null });

    try {
      // Build connection parameters as JSON string of name:value pairs
      const connectionParametersJson = JSON.stringify(connectionDetailValues);

      // Build credentials as JSON string of name:value pairs (if any)
      const hasCredentials =
        credFields.length > 0 && Object.keys(credentialValues).length > 0;
      const credentialsJson = hasCredentials
        ? JSON.stringify(credentialValues)
        : undefined;

      // Map UI connection mode to Fabric API connectivity type
      const connectivityTypeMap: Record<ConnectionMode, string> = {
        Cloud: "ShareableCloud",
        OnPremises: "OnPremisesGateway",
        VirtualNetwork: "VirtualNetworkGateway",
        StreamingVirtualNetwork: "VirtualNetworkGateway",
      };

      const toolArgs: Record<string, unknown> = {
        connectionName: connectionName.trim(),
        connectionType: selectedType?.dataSourceType || selectedDataSourceType,
        creationMethod: selectedType?.creationMethod,
        connectionParameters: connectionParametersJson,
        credentialType: selectedCredentialType,
        privacyLevel,
        connectionEncryption: encryptedConnection,
        skipTestConnection,
        connectivityType: connectivityTypeMap[connectionMode],
      };

      if (credentialsJson) {
        toolArgs.credentials = credentialsJson;
      }

      if (selectedGatewayId) {
        toolArgs.gatewayId = selectedGatewayId;
      }

      console.log("[CreateConnection] Creating connection:", toolArgs);

      // Uses Fabric REST API: POST https://api.fabric.microsoft.com/v1/connections
      const result = await this.callServerTool("create_connection", toolArgs);

      let parsed: CreateConnectionResult | null = null;
      if (typeof result === "string") {
        try {
          parsed = JSON.parse(result);
        } catch {
          parsed = { error: result };
        }
      } else if (result && typeof result === "object") {
        parsed = result as CreateConnectionResult;
      }

      if (parsed?.success && parsed.connectionId) {
        await this.updateModelContext({
          connectionId: parsed.connectionId,
          connectionName: connectionName.trim(),
          connectionType: selectedDataSourceType,
          connectionMode,
          timestamp: new Date().toISOString(),
        });

        this.setState({
          isSubmitting: false,
          createdConnectionId: parsed.connectionId,
          createdConnectionName: parsed.connectionName || connectionName.trim(),
        });
      } else {
        this.setState({
          isSubmitting: false,
          submitError:
            parsed?.message ||
            parsed?.error ||
            "Failed to create connection. Please try again.",
        });
      }
    } catch (error) {
      console.error("[CreateConnection] Submit error:", error);
      this.setState({
        isSubmitting: false,
        submitError: error instanceof Error ? error.message : "Unknown error",
      });
    }
  }

  // ===========================================================================
  // Helpers
  // ===========================================================================

  private getFilteredGateways(): Gateway[] {
    const { gateways, connectionMode } = this.state;
    // Fabric API returns: "OnPremises", "OnPremisesPersonal", "VirtualNetwork"
    const gatewayTypeMap: Record<ConnectionMode, string> = {
      OnPremises: "OnPremises",
      VirtualNetwork: "VirtualNetwork",
      StreamingVirtualNetwork: "VirtualNetwork",
      Cloud: "",
    };
    const targetType = gatewayTypeMap[connectionMode];
    if (!targetType) return [];
    return gateways.filter((gw) => gw.type === targetType);
  }

  private getSelectedTypeInfo(): SupportedDataSourceType | undefined {
    return this.state.supportedDataSourceTypes.find(
      (t) => t.displayName === this.state.selectedDataSourceType,
    );
  }

  private isFormValid(): boolean {
    const {
      connectionName,
      connectionMode,
      selectedGatewayId,
      selectedDataSourceType,
    } = this.state;

    if (!connectionName.trim()) return false;
    if (!selectedDataSourceType) return false;
    if (requiresGateway(connectionMode) && !selectedGatewayId) return false;

    return true;
  }

  // ===========================================================================
  // Render
  // ===========================================================================

  protected renderContent(): ReactNode {
    const {
      connectionMode,
      connectionName,
      selectedGatewayId,
      selectedDataSourceType,
      connectionDetailValues,
      selectedCredentialType,
      credentialValues,
      privacyLevel,
      encryptedConnection,
      skipTestConnection,
      isLoadingGateways,
      gatewayError,
      supportedDataSourceTypes,
      isLoadingConnectionTypes,
      connectionTypesError,
      isSubmitting,
      submitError,
      createdConnectionId,
      createdConnectionName,
    } = this.state;

    // Connecting state
    if (!this.state.isConnected) {
      return <LoadingSpinner message="Connecting to MCP host..." />;
    }

    // Success state
    if (createdConnectionId && createdConnectionName) {
      return (
        <div style={baseStyles.container}>
          <SuccessBanner
            connectionId={createdConnectionId}
            connectionName={createdConnectionName}
          />
        </div>
      );
    }

    const filteredGateways = this.getFilteredGateways();
    const showGatewayDropdown = requiresGateway(connectionMode);
    const selectedTypeInfo = this.getSelectedTypeInfo();
    const hasTypeSelected = !!selectedDataSourceType && !!selectedTypeInfo;
    const errorMessage =
      submitError || gatewayError || connectionTypesError || null;

    return (
      <div style={baseStyles.container}>
        <h1 style={baseStyles.h1}>New Connection</h1>

        {/* Error banner */}
        {errorMessage && (
          <ErrorBanner
            message={errorMessage}
            onRetry={
              gatewayError || connectionTypesError
                ? this.handleRetry
                : undefined
            }
          />
        )}

        {/* Connection mode tabs */}
        <ConnectionModeSelector
          selectedMode={connectionMode}
          onModeChange={this.handleModeChange}
          disabled={isSubmitting}
        />

        {/* Gateway dropdown (for non-cloud modes) */}
        {showGatewayDropdown && (
          <GatewayDropdown
            gateways={filteredGateways}
            selectedGatewayId={selectedGatewayId}
            onSelect={this.handleGatewaySelect}
            isLoading={isLoadingGateways}
            disabled={isSubmitting}
            label={
              connectionMode === "StreamingVirtualNetwork"
                ? "Streaming data gateway name"
                : "Gateway cluster name"
            }
          />
        )}

        {/* Connection name */}
        <ConnectionNameInput
          value={connectionName}
          onChange={this.handleNameChange}
          disabled={isSubmitting}
        />

        {/* Connection type dropdown */}
        <ConnectionTypeDropdown
          dataSourceTypes={supportedDataSourceTypes}
          selectedType={selectedDataSourceType}
          onSelect={this.handleDataSourceTypeChange}
          isLoading={isLoadingConnectionTypes}
          disabled={isSubmitting}
        />

        {/* Dynamic connection detail fields (shown when type is selected) */}
        {hasTypeSelected && (
          <ConnectionDetailFields
            labels={selectedTypeInfo.labels}
            values={connectionDetailValues}
            onChange={this.handleConnectionDetailChange}
            disabled={isSubmitting}
          />
        )}

        {/* Credential section (shown when type is selected) */}
        {hasTypeSelected && (
          <CredentialSection
            availableCredentialTypes={selectedTypeInfo.credentialTypes}
            selectedCredentialType={selectedCredentialType}
            credentialValues={credentialValues}
            privacyLevel={privacyLevel}
            encryptedConnection={encryptedConnection}
            skipTestConnection={skipTestConnection}
            isEncryptedConnectionSupported={
              selectedTypeInfo.supportedEncryptionTypes.length > 0
            }
            isSkipTestConnectionSupported={
              selectedTypeInfo.isSkipTestConnectionSupported
            }
            onCredentialTypeChange={this.handleCredentialTypeChange}
            onCredentialValueChange={this.handleCredentialValueChange}
            onPrivacyLevelChange={this.handlePrivacyLevelChange}
            onEncryptedConnectionChange={this.handleEncryptedConnectionChange}
            onSkipTestConnectionChange={this.handleSkipTestConnectionChange}
            disabled={isSubmitting}
          />
        )}

        {/* Action buttons */}
        <FormButtons
          onSubmit={this.handleSubmit}
          onCancel={this.handleCancel}
          isSubmitting={isSubmitting}
          submitDisabled={!this.isFormValid()}
        />
      </div>
    );
  }
}

export default CreateConnectionApp;
