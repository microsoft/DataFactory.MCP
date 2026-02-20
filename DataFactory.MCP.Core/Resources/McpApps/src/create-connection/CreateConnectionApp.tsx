/**
 * CreateConnectionApp - Wizard orchestrator (3 steps).
 *
 *   Step 0 — Mode:        pick connectivity type (auto-advances on click)
 *   Step 1 — Details:     gateway, name, type, detail fields
 *   Step 2 — Credentials: auth method, credential fields, privacy, encryption
 *
 * Responsibilities:
 *   - Owns all wizard state
 *   - Delegates data fetching to connectionDataService
 *   - Delegates validation to connectionValidation
 *   - Delegates rendering to ModeStep / DetailsStep / CredentialsStep
 */

import { ReactNode } from "react";
import {
  McpAppComponent,
  McpAppComponentProps,
  McpAppComponentState,
  BaseStyles,
  applyBodyStyles,
} from "../shared";
import {
  ConnectionMode,
  CredentialType,
  PrivacyLevel,
  SupportedDataSourceType,
  CreateConnectionResult,
  Gateway,
} from "./services/types";
import {
  fetchGateways,
  fetchSupportedConnectionTypes,
} from "./services/connectionDataService";
import { filterGatewaysByMode, findSelectedType } from "./helpers";
import {
  validateDetails,
  validateCredentials,
} from "./validation/connectionValidation";
import { WizardStep, WIZARD_STEPS } from "./wizard/types";
import { WizardStepIndicator } from "./wizard/WizardStepIndicator";
import { ModeStep } from "./wizard/ModeStep";
import { DetailsStep } from "./wizard/DetailsStep";
import { CredentialsStep } from "./wizard/CredentialsStep";
import { SuccessBanner, LoadingSpinner } from "./components";

// =============================================================================
// State
// =============================================================================

interface CreateConnectionAppState extends McpAppComponentState {
  currentStep: WizardStep;

  // Form — basic
  connectionMode: ConnectionMode;
  connectionName: string;
  selectedGatewayId: string | null;

  // Form — data source type
  selectedDataSourceType: string;
  connectionDetailValues: Record<string, string>;

  // Form — credentials
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

const INITIAL_FORM_STATE = {
  currentStep: "mode" as WizardStep,
  connectionMode: "Cloud" as ConnectionMode,
  connectionName: "",
  selectedGatewayId: null,
  selectedDataSourceType: "",
  connectionDetailValues: {},
  selectedCredentialType: "Anonymous" as CredentialType,
  credentialValues: {},
  privacyLevel: "None" as PrivacyLevel,
  encryptedConnection: "NotEncrypted",
  skipTestConnection: false,
  submitError: null,
};

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
      ...INITIAL_FORM_STATE,
      gateways: [],
      isLoadingGateways: false,
      gatewayError: null,
      supportedDataSourceTypes: [],
      isLoadingConnectionTypes: false,
      connectionTypesError: null,
      isSubmitting: false,
      createdConnectionId: null,
      createdConnectionName: null,
    };

    const methods = [
      "handleModeChange",
      "handleGatewaySelect",
      "handleNameChange",
      "handleDataSourceTypeChange",
      "handleConnectionDetailChange",
      "handleCredentialTypeChange",
      "handleCredentialValueChange",
      "handlePrivacyLevelChange",
      "handleEncryptedConnectionChange",
      "handleSkipTestConnectionChange",
      "handleSubmit",
      "handleCancel",
      "handleRetry",
      "handleNext",
      "handleBack",
      "handleStepClick",
    ] as const;
    methods.forEach((m) => {
      (this as unknown as Record<string, unknown>)[m] = (
        this as unknown as Record<string, (...args: unknown[]) => unknown>
      )[m].bind(this);
    });
  }

  componentDidMount(): void {
    super.componentDidMount();
    applyBodyStyles();
  }

  // ===========================================================================
  // MCP lifecycle
  // ===========================================================================

  protected onToolResult(_result: unknown): void {
    console.log("[CreateConnection] Connected, fetching data...");
    this.loadGateways();
    this.loadConnectionTypes();
  }

  // ===========================================================================
  // Data loading (delegates to connectionDataService)
  // ===========================================================================

  private async loadGateways(): Promise<void> {
    this.setState({ isLoadingGateways: true, gatewayError: null });
    try {
      const gateways = await fetchGateways(this.callServerTool.bind(this));
      this.setState({ gateways, isLoadingGateways: false });
    } catch (error) {
      this.setState({
        gatewayError:
          error instanceof Error ? error.message : "Failed to load gateways",
        isLoadingGateways: false,
      });
    }
  }

  private async loadConnectionTypes(): Promise<void> {
    this.setState({
      isLoadingConnectionTypes: true,
      connectionTypesError: null,
    });
    try {
      const supportedDataSourceTypes = await fetchSupportedConnectionTypes(
        this.callServerTool.bind(this),
      );
      this.setState({
        supportedDataSourceTypes,
        isLoadingConnectionTypes: false,
      });
    } catch (error) {
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
  // Wizard navigation
  // ===========================================================================

  private handleModeChange(mode: ConnectionMode): void {
    this.setState({
      connectionMode: mode,
      selectedGatewayId: null,
      submitError: null,
      currentStep: "details",
    });
  }

  private handleNext(): void {
    const error = validateDetails({
      connectionName: this.state.connectionName,
      connectionMode: this.state.connectionMode,
      selectedGatewayId: this.state.selectedGatewayId,
      selectedDataSourceType: this.state.selectedDataSourceType,
      connectionDetailValues: this.state.connectionDetailValues,
      selectedTypeInfo: findSelectedType(
        this.state.supportedDataSourceTypes,
        this.state.selectedDataSourceType,
      ),
    });
    if (error) {
      this.setState({ submitError: error });
    } else {
      this.setState({ currentStep: "credentials", submitError: null });
    }
  }

  private handleBack(): void {
    this.setState((prev) => ({
      currentStep:
        WIZARD_STEPS[Math.max(0, WIZARD_STEPS.indexOf(prev.currentStep) - 1)],
      submitError: null,
    }));
  }

  private handleStepClick(step: WizardStep): void {
    if (
      WIZARD_STEPS.indexOf(step) < WIZARD_STEPS.indexOf(this.state.currentStep)
    ) {
      this.setState({ currentStep: step, submitError: null });
    }
  }

  private handleCancel(): void {
    this.setState({ ...INITIAL_FORM_STATE });
  }

  private handleRetry(): void {
    this.setState({
      submitError: null,
      gatewayError: null,
      connectionTypesError: null,
    });
    this.loadGateways();
    this.loadConnectionTypes();
  }

  // ===========================================================================
  // Field change handlers
  // ===========================================================================

  private handleGatewaySelect(gatewayId: string): void {
    this.setState({ selectedGatewayId: gatewayId, submitError: null });
  }

  private handleNameChange(value: string): void {
    this.setState({ connectionName: value, submitError: null });
  }

  private handleDataSourceTypeChange(dsType: string): void {
    const selectedType = this.state.supportedDataSourceTypes.find(
      (t) => t.displayName === dsType,
    );
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
      credentialValues: { ...prev.credentialValues, [fieldName]: value },
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

    const credError = validateCredentials({
      selectedCredentialType,
      credentialValues,
    });
    if (credError) {
      this.setState({ submitError: credError });
      return;
    }

    this.setState({ isSubmitting: true, submitError: null });

    try {
      const selectedType = findSelectedType(
        this.state.supportedDataSourceTypes,
        this.state.selectedDataSourceType,
      );
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
        connectionParameters: JSON.stringify(connectionDetailValues),
        credentialType: selectedCredentialType,
        privacyLevel,
        connectionEncryption: encryptedConnection,
        skipTestConnection,
        connectivityType: connectivityTypeMap[connectionMode],
      };

      if (Object.keys(credentialValues).length > 0) {
        toolArgs.credentials = JSON.stringify(credentialValues);
      }
      if (selectedGatewayId) toolArgs.gatewayId = selectedGatewayId;

      console.log("[CreateConnection] Creating connection:", toolArgs);

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

      if (parsed?.success && parsed.connection?.id) {
        await this.updateModelContext({
          connectionId: parsed.connection.id,
          connectionName: connectionName.trim(),
          connectionType: selectedDataSourceType,
          connectionMode,
          timestamp: new Date().toISOString(),
        });
        this.setState({
          isSubmitting: false,
          createdConnectionId: parsed.connection.id,
          createdConnectionName:
            parsed.connection.displayName || connectionName.trim(),
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
        <div style={BaseStyles.container}>
          <SuccessBanner
            connectionId={createdConnectionId}
            connectionName={createdConnectionName}
          />
        </div>
      );
    }

    const selectedTypeInfo = findSelectedType(
      this.state.supportedDataSourceTypes,
      this.state.selectedDataSourceType,
    );

    return (
      <div style={BaseStyles.container}>
        <h1 style={BaseStyles.h1}>New Connection</h1>
        <WizardStepIndicator
          currentStep={currentStep}
          onStepClick={this.handleStepClick}
        />
        {currentStep === "mode" && (
          <ModeStep
            connectionMode={this.state.connectionMode}
            onModeChange={this.handleModeChange}
          />
        )}
        {currentStep === "details" && (
          <DetailsStep
            connectionMode={this.state.connectionMode}
            connectionName={this.state.connectionName}
            selectedGatewayId={this.state.selectedGatewayId}
            selectedDataSourceType={this.state.selectedDataSourceType}
            connectionDetailValues={this.state.connectionDetailValues}
            selectedTypeInfo={selectedTypeInfo}
            gateways={filterGatewaysByMode(
              this.state.gateways,
              this.state.connectionMode,
            )}
            isLoadingGateways={this.state.isLoadingGateways}
            supportedDataSourceTypes={this.state.supportedDataSourceTypes}
            isLoadingConnectionTypes={this.state.isLoadingConnectionTypes}
            errorMessage={
              this.state.submitError ||
              this.state.gatewayError ||
              this.state.connectionTypesError ||
              null
            }
            isSubmitting={this.state.isSubmitting}
            onGatewaySelect={this.handleGatewaySelect}
            onNameChange={this.handleNameChange}
            onDataSourceTypeChange={this.handleDataSourceTypeChange}
            onConnectionDetailChange={this.handleConnectionDetailChange}
            onNext={this.handleNext}
            onBack={this.handleBack}
            onCancel={this.handleCancel}
            onRetry={this.handleRetry}
          />
        )}
        {currentStep === "credentials" && selectedTypeInfo && (
          <CredentialsStep
            selectedTypeInfo={selectedTypeInfo}
            selectedCredentialType={this.state.selectedCredentialType}
            credentialValues={this.state.credentialValues}
            privacyLevel={this.state.privacyLevel}
            encryptedConnection={this.state.encryptedConnection}
            skipTestConnection={this.state.skipTestConnection}
            isSubmitting={this.state.isSubmitting}
            submitError={this.state.submitError}
            onCredentialTypeChange={this.handleCredentialTypeChange}
            onCredentialValueChange={this.handleCredentialValueChange}
            onPrivacyLevelChange={this.handlePrivacyLevelChange}
            onEncryptedConnectionChange={this.handleEncryptedConnectionChange}
            onSkipTestConnectionChange={this.handleSkipTestConnectionChange}
            onSubmit={this.handleSubmit}
            onBack={this.handleBack}
            onCancel={this.handleCancel}
          />
        )}
      </div>
    );
  }
}

export default CreateConnectionApp;
