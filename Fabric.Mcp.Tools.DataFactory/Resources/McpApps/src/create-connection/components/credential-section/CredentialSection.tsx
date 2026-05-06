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
} from "../../services/types";
import { BaseStyles } from "../../../shared";
import { CredentialSectionStyles as styles } from "./CredentialSection.styles";
import type { CredentialSectionProps } from "./types";

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
        <div style={BaseStyles.formGroup}>
          <label htmlFor="auth-method" style={BaseStyles.label}>
            Authentication method <span style={styles.required}>*</span>
          </label>
          <select
            id="auth-method"
            value={selectedCredentialType}
            onChange={this.handleCredentialTypeChange}
            disabled={disabled}
            style={BaseStyles.select}
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
          <div key={field.name} style={BaseStyles.formGroup}>
            <label htmlFor={`cred-${field.name}`} style={BaseStyles.label}>
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
              style={BaseStyles.input}
              autoComplete={field.isSecret ? "new-password" : "off"}
            />
          </div>
        ))}

        {/* Privacy level dropdown */}
        <div style={BaseStyles.formGroup}>
          <label htmlFor="privacy-level" style={BaseStyles.label}>
            Privacy level
          </label>
          <select
            id="privacy-level"
            value={privacyLevel}
            onChange={this.handlePrivacyChange}
            disabled={disabled}
            style={BaseStyles.select}
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
          <div style={BaseStyles.formGroup}>
            <label htmlFor="encrypted-conn" style={BaseStyles.label}>
              Encrypted connection
            </label>
            <select
              id="encrypted-conn"
              value={encryptedConnection}
              onChange={this.handleEncryptedChange}
              disabled={disabled}
              style={BaseStyles.select}
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
