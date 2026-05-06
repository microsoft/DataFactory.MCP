/**
 * DetailsStep - Second wizard step: gateway, connection name, type, and
 * dynamic detail fields. Advances to credentials on "Next".
 */

import {
  ConnectionMode,
  Gateway,
  SupportedDataSourceType,
  requiresGateway,
} from "../services/types";
import {
  GatewayDropdown,
  ConnectionNameInput,
  ConnectionTypeDropdown,
  ConnectionDetailFields,
  FormButtons,
  ErrorBanner,
} from "../components";

export interface DetailsStepProps {
  // Form values
  connectionMode: ConnectionMode;
  connectionName: string;
  selectedGatewayId: string | null;
  selectedDataSourceType: string;
  connectionDetailValues: Record<string, string>;
  selectedTypeInfo: SupportedDataSourceType | undefined;

  // Loaded data
  gateways: Gateway[];
  isLoadingGateways: boolean;
  supportedDataSourceTypes: SupportedDataSourceType[];
  isLoadingConnectionTypes: boolean;

  // Status
  errorMessage: string | null;
  isSubmitting: boolean;

  // Handlers
  onGatewaySelect: (id: string) => void;
  onNameChange: (value: string) => void;
  onDataSourceTypeChange: (type: string) => void;
  onConnectionDetailChange: (field: string, value: string) => void;
  onNext: () => void;
  onBack: () => void;
  onCancel: () => void;
  onRetry?: () => void;
}

export function DetailsStep(props: DetailsStepProps) {
  const {
    connectionMode,
    connectionName,
    selectedGatewayId,
    selectedDataSourceType,
    connectionDetailValues,
    selectedTypeInfo,
    gateways,
    isLoadingGateways,
    supportedDataSourceTypes,
    isLoadingConnectionTypes,
    errorMessage,
    isSubmitting,
    onGatewaySelect,
    onNameChange,
    onDataSourceTypeChange,
    onConnectionDetailChange,
    onNext,
    onBack,
    onCancel,
    onRetry,
  } = props;

  const showGatewayDropdown = requiresGateway(connectionMode);
  const hasTypeSelected = !!selectedDataSourceType && !!selectedTypeInfo;

  return (
    <>
      {errorMessage && <ErrorBanner message={errorMessage} onRetry={onRetry} />}

      {showGatewayDropdown && (
        <GatewayDropdown
          gateways={gateways}
          selectedGatewayId={selectedGatewayId}
          onSelect={onGatewaySelect}
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
        onChange={onNameChange}
        disabled={isSubmitting}
      />

      <ConnectionTypeDropdown
        dataSourceTypes={supportedDataSourceTypes}
        selectedType={selectedDataSourceType}
        onSelect={onDataSourceTypeChange}
        isLoading={isLoadingConnectionTypes}
        disabled={isSubmitting}
      />

      {hasTypeSelected && (
        <ConnectionDetailFields
          labels={selectedTypeInfo!.labels}
          values={connectionDetailValues}
          onChange={onConnectionDetailChange}
          disabled={isSubmitting}
        />
      )}

      <FormButtons
        onBack={onBack}
        onSubmit={onNext}
        onCancel={onCancel}
        isSubmitting={false}
        submitDisabled={false}
        submitLabel="Next"
      />
    </>
  );
}
