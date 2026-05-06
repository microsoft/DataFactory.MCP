/**
 * Converts the Fabric API ListSupportedConnectionTypes response
 * into UI-friendly SupportedDataSourceType[] entries.
 *
 * Each (type, creationMethod) pair becomes one entry.
 * If a type has only one creation method, displayName = type.
 * If multiple, displayName = "type (creationMethod)".
 */

import {
  SupportedConnectionType,
  SupportedDataSourceType,
  DataSourceLabel,
} from "./types";

export function convertConnectionTypes(
  apiTypes: SupportedConnectionType[],
): SupportedDataSourceType[] {
  const result: SupportedDataSourceType[] = [];

  for (const ct of apiTypes) {
    const hasMultipleMethods = ct.creationMethods.length > 1;

    for (const cm of ct.creationMethods) {
      const labels: DataSourceLabel[] = cm.parameters.map((p) => ({
        name: p.name,
        dataType: p.dataType,
        required: p.required,
        allowedValues: p.allowedValues,
      }));

      result.push({
        dataSourceType: ct.type,
        creationMethod: cm.name,
        displayName: hasMultipleMethods ? `${ct.type} (${cm.name})` : ct.type,
        labels,
        credentialTypes: ct.supportedCredentialTypes,
        supportedEncryptionTypes: ct.supportedConnectionEncryptionTypes,
        isSkipTestConnectionSupported: ct.supportsSkipTestConnection,
      });
    }
  }

  return result;
}
