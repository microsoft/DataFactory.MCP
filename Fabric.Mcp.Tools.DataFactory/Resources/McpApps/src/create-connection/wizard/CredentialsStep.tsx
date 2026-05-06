/**
 * CredentialsStep - Third wizard step: authentication method, credential
 * fields, privacy level, and encryption. Submits on "Create".
 */

import {
  CredentialType,
  PrivacyLevel,
  SupportedDataSourceType,
} from "../services/types";
import { CredentialSection, FormButtons, ErrorBanner } from "../components";

export interface CredentialsStepProps {
  // From selected type
  selectedTypeInfo: SupportedDataSourceType;

  // Credential form values
  selectedCredentialType: CredentialType;
  credentialValues: Record<string, string>;
  privacyLevel: PrivacyLevel;
  encryptedConnection: string;
  skipTestConnection: boolean;

  // Status
  isSubmitting: boolean;
  submitError: string | null;

  // Handlers
  onCredentialTypeChange: (type: CredentialType) => void;
  onCredentialValueChange: (field: string, value: string) => void;
  onPrivacyLevelChange: (level: PrivacyLevel) => void;
  onEncryptedConnectionChange: (value: string) => void;
  onSkipTestConnectionChange: (value: boolean) => void;
  onSubmit: () => void;
  onBack: () => void;
  onCancel: () => void;
}

export function CredentialsStep(props: CredentialsStepProps) {
  const {
    selectedTypeInfo,
    selectedCredentialType,
    credentialValues,
    privacyLevel,
    encryptedConnection,
    skipTestConnection,
    isSubmitting,
    submitError,
    onCredentialTypeChange,
    onCredentialValueChange,
    onPrivacyLevelChange,
    onEncryptedConnectionChange,
    onSkipTestConnectionChange,
    onSubmit,
    onBack,
    onCancel,
  } = props;

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
        onCredentialTypeChange={onCredentialTypeChange}
        onCredentialValueChange={onCredentialValueChange}
        onPrivacyLevelChange={onPrivacyLevelChange}
        onEncryptedConnectionChange={onEncryptedConnectionChange}
        onSkipTestConnectionChange={onSkipTestConnectionChange}
        disabled={isSubmitting}
      />

      <FormButtons
        onBack={onBack}
        onSubmit={onSubmit}
        onCancel={onCancel}
        isSubmitting={isSubmitting}
        submitDisabled={false}
        submitLabel="Create"
      />
    </>
  );
}
