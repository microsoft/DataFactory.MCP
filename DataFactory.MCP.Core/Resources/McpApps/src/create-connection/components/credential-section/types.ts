/**
 * Types for the CredentialSection component.
 */

import { CredentialType, PrivacyLevel } from "../../services/types";

export interface CredentialSectionProps {
  availableCredentialTypes: string[];
  selectedCredentialType: CredentialType;
  credentialValues: Record<string, string>;
  privacyLevel: PrivacyLevel;
  encryptedConnection: string;
  skipTestConnection: boolean;
  isEncryptedConnectionSupported: boolean;
  isSkipTestConnectionSupported: boolean;
  onCredentialTypeChange: (type: CredentialType) => void;
  onCredentialValueChange: (fieldName: string, value: string) => void;
  onPrivacyLevelChange: (level: PrivacyLevel) => void;
  onEncryptedConnectionChange: (value: string) => void;
  onSkipTestConnectionChange: (value: boolean) => void;
  disabled?: boolean;
}
