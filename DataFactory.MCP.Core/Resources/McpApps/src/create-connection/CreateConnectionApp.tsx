/**
 * CreateConnectionApp - Main Application Component
 *
 * 3-step wizard:
 *   Step 0 — Mode:        pick connection mode (auto-advances on click)
 *   Step 1 — Details:     gateway, name, type, detail fields
 *   Step 2 — Credentials: auth method, credential fields, privacy, encryption
 *
 * All components are React class components.
 */

import { ReactNode, CSSProperties } from "react";
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

type WizardStep = 0 | 1 | 2;

interface CreateConnectionAppState extends McpAppComponentState {
  // Wizard
  currentStep: WizardStep;

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

      currentStep: 0,

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
    this.handleNext = this.handleNext.bind(this);
    this.handleBack = this.handleBack.bind(this);
    this.handleStepClick = this.handleStepClick.bind(this);
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
      currentStep: 1,
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
      currentStep: 0,
      connectionMode: "Cloud",
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

  private handleNext(): void {
    if (this.isStep1Valid()) {
      this.setState({ currentStep: 2, submitError: null });
    } else {
      // surface the first missing field as an error
      const {
        connectionName,
        connectionMode,
        selectedGatewayId,
        selectedDataSourceType,
        connectionDetailValues,
      } = this.state;
      if (!connectionName.trim()) {
        this.setState({ submitError: "Connection name is required" });
        return;
      }
      if (!selectedDataSourceType) {
        this.setState({ submitError: "Please select a connection type" });
        return;
      }
      if (requiresGateway(connectionMode) && !selectedGatewayId) {
        this.setState({ submitError: "Please select a gateway" });
        return;
      }
      const selectedType = this.getSelectedTypeInfo();
      if (selectedType) {
        for (const label of selectedType.labels) {
          if (label.required && !connectionDetailValues[label.name]?.trim()) {
            this.setState({ submitError: `${label.name} is required` });
            return;
          }
        }
      }
    }
  }

  private handleBack(): void {
    this.setState((prev) => ({
      currentStep: Math.max(0, prev.currentStep - 1) as WizardStep,
      submitError: null,
    }));
  }

  private handleStepClick(step: WizardStep): void {
    if (step < this.state.currentStep) {
      this.setState({ currentStep: step, submitError: null });
    }
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

  private isStep1Valid(): boolean {
    const {
      connectionName,
      connectionMode,
      selectedGatewayId,
      selectedDataSourceType,
      connectionDetailValues,
    } = this.state;
    if (!connectionName.trim()) return false;
    if (!selectedDataSourceType) return false;
    if (requiresGateway(connectionMode) && !selectedGatewayId) return false;
    const selectedType = this.getSelectedTypeInfo();
    if (selectedType) {
      for (const label of selectedType.labels) {
        if (label.required && !connectionDetailValues[label.name]?.trim())
          return false;
      }
    }
    return true;
  }

  // ===========================================================================
  // Render helpers
  // ===========================================================================

  private renderStepIndicator(): ReactNode {
    const { currentStep } = this.state;
    const steps: [WizardStep, string][] = [
      [0, "Mode"],
      [1, "Details"],
      [2, "Credentials"],
    ];
    const dividerStyle: CSSProperties = {
      color: "var(--vscode-descriptionForeground, #888)",
      margin: "0 3px",
      fontSize: "0.7rem",
    };
    return (
      <div
        style={{
          display: "flex",
          alignItems: "center",
          marginBottom: "12px",
          fontSize: "0.75rem",
        }}
      >
        {steps.map(([idx, label], i) => (
          <span key={idx} style={{ display: "flex", alignItems: "center" }}>
            {i > 0 && <span style={dividerStyle}>›</span>}
            <button
              type="button"
              onClick={() => this.handleStepClick(idx)}
              disabled={idx >= currentStep}
              style={{
                background: "none",
                border: "none",
                padding: "2px 4px",
                cursor: idx < currentStep ? "pointer" : "default",
                color:
                  idx === currentStep
                    ? "var(--vscode-foreground, #ccc)"
                    : idx < currentStep
                      ? "var(--vscode-textLink-foreground, #3794ff)"
                      : "var(--vscode-descriptionForeground, #888)",
                fontWeight: idx === currentStep ? 600 : 400,
                fontSize: "0.75rem",
                textDecoration: idx < currentStep ? "underline" : "none",
              }}
            >
              {label}
            </button>
          </span>
        ))}
      </div>
    );
  }

  private renderStep0(): ReactNode {
    return (
      <>
        <p
          style={{
            fontSize: "0.8rem",
            color: "var(--vscode-descriptionForeground, #888)",
            marginBottom: "10px",
            marginTop: 0,
          }}
        >
          Choose a connectivity type to get started.
        </p>
        <ConnectionModeSelector
          selectedMode={this.state.connectionMode}
          onModeChange={this.handleModeChange}
          disabled={false}
        />
      </>
    );
  }

  private renderStep1(): ReactNode {
    const {
      connectionMode,
      connectionName,
      selectedGatewayId,
      selectedDataSourceType,
      connectionDetailValues,
      isLoadingGateways,
      gatewayError,
      supportedDataSourceTypes,
      isLoadingConnectionTypes,
      connectionTypesError,
      isSubmitting,
      submitError,
    } = this.state;

    const filteredGateways = this.getFilteredGateways();
    const showGatewayDropdown = requiresGateway(connectionMode);
    const selectedTypeInfo = this.getSelectedTypeInfo();
    const hasTypeSelected = !!selectedDataSourceType && !!selectedTypeInfo;
    const errorMessage =
      submitError || gatewayError || connectionTypesError || null;

    return (
      <>
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

        <ConnectionNameInput
          value={connectionName}
          onChange={this.handleNameChange}
          disabled={isSubmitting}
        />

        <ConnectionTypeDropdown
          dataSourceTypes={supportedDataSourceTypes}
          selectedType={selectedDataSourceType}
          onSelect={this.handleDataSourceTypeChange}
          isLoading={isLoadingConnectionTypes}
          disabled={isSubmitting}
        />

        {hasTypeSelected && (
          <ConnectionDetailFields
            labels={selectedTypeInfo.labels}
            values={connectionDetailValues}
            onChange={this.handleConnectionDetailChange}
            disabled={isSubmitting}
          />
        )}

        <FormButtons
          onBack={this.handleBack}
          onSubmit={this.handleNext}
          onCancel={this.handleCancel}
          isSubmitting={false}
          submitDisabled={false}
          submitLabel="Next"
        />
      </>
    );
  }

  private renderStep2(): ReactNode {
    const {
      selectedDataSourceType,
      selectedCredentialType,
      credentialValues,
      privacyLevel,
      encryptedConnection,
      skipTestConnection,
      isSubmitting,
      submitError,
    } = this.state;

    const selectedTypeInfo = this.getSelectedTypeInfo();
    if (!selectedTypeInfo) {
      this.setState({ currentStep: 1 });
      return null;
    }

    return (
      <>
        {submitError && <ErrorBanner message={submitError} />}

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

        <FormButtons
          onBack={this.handleBack}
          onSubmit={this.handleSubmit}
          onCancel={this.handleCancel}
          isSubmitting={isSubmitting}
          submitDisabled={false}
          submitLabel="Create"
        />
      </>
    );
  }

  // ===========================================================================
  // Render
  // ===========================================================================

  protected renderContent(): ReactNode {
    const { currentStep, createdConnectionId, createdConnectionName } =
      this.state;

    if (!this.state.isConnected) {
      return <LoadingSpinner message="Connecting to MCP host..." />;
    }

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

    return (
      <div style={baseStyles.container}>
        <h1 style={baseStyles.h1}>New Connection</h1>
        {this.renderStepIndicator()}
        {currentStep === 0 && this.renderStep0()}
        {currentStep === 1 && this.renderStep1()}
        {currentStep === 2 && this.renderStep2()}
      </div>
    );
  }
}

export default CreateConnectionApp;
