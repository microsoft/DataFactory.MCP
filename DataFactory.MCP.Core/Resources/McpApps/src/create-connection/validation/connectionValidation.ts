/**
 * Pure validation functions for the Create Connection wizard.
 * No side effects — each function returns the first error string found, or null.
 */

import {
  ConnectionMode,
  CredentialType,
  SupportedDataSourceType,
  credentialFieldsMap,
  requiresGateway,
} from "../services/types";

// =============================================================================
// Input shapes
// =============================================================================

export interface DetailsValidationInput {
  connectionName: string;
  connectionMode: ConnectionMode | null;
  selectedGatewayId: string | null;
  selectedDataSourceType: string;
  connectionDetailValues: Record<string, string>;
  selectedTypeInfo: SupportedDataSourceType | undefined;
}

export interface CredentialsValidationInput {
  selectedCredentialType: CredentialType;
  credentialValues: Record<string, string>;
}

// =============================================================================
// Validators
// =============================================================================

export function validateDetails(input: DetailsValidationInput): string | null {
  const {
    connectionName,
    connectionMode,
    selectedGatewayId,
    selectedDataSourceType,
    connectionDetailValues,
    selectedTypeInfo,
  } = input;

  if (!connectionName.trim()) return "Connection name is required";
  if (!selectedDataSourceType) return "Please select a connection type";
  if (connectionMode != null && requiresGateway(connectionMode) && !selectedGatewayId)
    return "Please select a gateway";

  if (selectedTypeInfo) {
    for (const label of selectedTypeInfo.labels) {
      if (label.required && !connectionDetailValues[label.name]?.trim()) {
        return `${label.name} is required`;
      }
    }
  }

  return null;
}

export function validateCredentials(
  input: CredentialsValidationInput,
): string | null {
  const { selectedCredentialType, credentialValues } = input;
  const credFields = credentialFieldsMap[selectedCredentialType] || [];

  for (const field of credFields) {
    if (!credentialValues[field.name]?.trim()) {
      return `${field.label} is required`;
    }
  }

  return null;
}
