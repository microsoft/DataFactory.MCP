/**
 * CredentialSection - Authentication method selector + credential input fields.
 * Renders a dropdown for credential type plus the appropriate fields
 * (e.g., username/password for Basic, key for Key, etc.).
 * Class component.
 */

import { Component, ReactNode } from "react";
import {
  CredentialType,
  credentialFieldsMap,
  PrivacyLevel,
  privacyLevelOptions,
} from "../services/types";
import { baseStyles } from "../../shared";

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

export class CredentialSection extends Component<CredentialSectionProps> {
  constructor(props: CredentialSectionProps) {
    super(props);
    this.handleCredentialTypeChange =
      this.handleCredentialTypeChange.bind(this);
    this.handlePrivacyChange = this.handlePrivacyChange.bind(this);
    this.handleEncryptedChange = this.handleEncryptedChange.bind(this);
    this.handleSkipTestChange = this.handleSkipTestChange.bind(this);
  }

  private handleCredentialTypeChange(
    e: React.ChangeEvent<HTMLSelectElement>,
  ): void {
    this.props.onCredentialTypeChange(e.target.value as CredentialType);
  }

  private handlePrivacyChange(e: React.ChangeEvent<HTMLSelectElement>): void {
    this.props.onPrivacyLevelChange(e.target.value as PrivacyLevel);
  }

  private handleEncryptedChange(e: React.ChangeEvent<HTMLSelectElement>): void {
    this.props.onEncryptedConnectionChange(e.target.value);
  }

  private handleSkipTestChange(e: React.ChangeEvent<HTMLInputElement>): void {
    this.props.onSkipTestConnectionChange(e.target.checked);
  }

  render(): ReactNode {
    const {
      availableCredentialTypes,
      selectedCredentialType,
      credentialValues,
      privacyLevel,
      encryptedConnection,
      skipTestConnection,
      isEncryptedConnectionSupported,
      isSkipTestConnectionSupported,
      disabled = false,
    } = this.props;

    const fields = credentialFieldsMap[selectedCredentialType] || [];

    return (
      <div style={styles.section}>
        <div style={styles.sectionHeader}>Credentials</div>

        {/* Authentication method dropdown */}
        <div style={baseStyles.formGroup}>
          <label htmlFor="auth-method" style={baseStyles.label}>
            Authentication method <span style={styles.required}>*</span>
          </label>
          <select
            id="auth-method"
            value={selectedCredentialType}
            onChange={this.handleCredentialTypeChange}
            disabled={disabled}
            style={baseStyles.select}
          >
            {availableCredentialTypes.map((ct) => (
              <option key={ct} value={ct}>
                {ct}
              </option>
            ))}
          </select>
        </div>

        {/* Dynamic credential fields */}
        {fields.map((field) => (
          <div key={field.name} style={baseStyles.formGroup}>
            <label htmlFor={`cred-${field.name}`} style={baseStyles.label}>
              {field.label} <span style={styles.required}>*</span>
            </label>
            <input
              id={`cred-${field.name}`}
              type={field.isSecret ? "password" : "text"}
              value={credentialValues[field.name] || ""}
              onChange={(e) =>
                this.props.onCredentialValueChange(field.name, e.target.value)
              }
              disabled={disabled}
              placeholder={`Enter ${field.label.toLowerCase()}`}
              style={baseStyles.input}
              autoComplete={field.isSecret ? "new-password" : "off"}
            />
          </div>
        ))}

        {/* Privacy level dropdown */}
        <div style={baseStyles.formGroup}>
          <label htmlFor="privacy-level" style={baseStyles.label}>
            Privacy level
          </label>
          <select
            id="privacy-level"
            value={privacyLevel}
            onChange={this.handlePrivacyChange}
            disabled={disabled}
            style={baseStyles.select}
          >
            {privacyLevelOptions.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        </div>

        {/* Encrypted connection dropdown */}
        {isEncryptedConnectionSupported && (
          <div style={baseStyles.formGroup}>
            <label htmlFor="encrypted-conn" style={baseStyles.label}>
              Encrypted connection
            </label>
            <select
              id="encrypted-conn"
              value={encryptedConnection}
              onChange={this.handleEncryptedChange}
              disabled={disabled}
              style={baseStyles.select}
            >
              <option value="NotEncrypted">Not Encrypted</option>
              <option value="Encrypted">Encrypted</option>
              <option value="Any">Any</option>
            </select>
          </div>
        )}

        {/* Skip test connection checkbox */}
        {isSkipTestConnectionSupported && (
          <div style={styles.checkboxGroup}>
            <label style={styles.checkboxLabel}>
              <input
                type="checkbox"
                checked={skipTestConnection}
                onChange={this.handleSkipTestChange}
                disabled={disabled}
                style={styles.checkbox}
              />
              Skip test connection
            </label>
          </div>
        )}
      </div>
    );
  }
}

const styles: Record<string, React.CSSProperties> = {
  section: {
    marginTop: "16px",
    padding: "16px",
    border: "1px solid var(--vscode-input-border, #3c3c3c)",
    borderRadius: "4px",
  },
  sectionHeader: {
    fontSize: "0.9rem",
    fontWeight: 600,
    marginBottom: "12px",
    color: "var(--vscode-foreground, #cccccc)",
  },
  required: { color: "var(--vscode-errorForeground, #f48771)" },
  checkboxGroup: {
    marginTop: "12px",
  },
  checkboxLabel: {
    display: "flex",
    alignItems: "center",
    gap: "8px",
    cursor: "pointer",
    fontSize: "0.875rem",
    color: "var(--vscode-foreground, #cccccc)",
  },
  checkbox: {
    accentColor: "var(--vscode-focusBorder, #007acc)",
  },
};
