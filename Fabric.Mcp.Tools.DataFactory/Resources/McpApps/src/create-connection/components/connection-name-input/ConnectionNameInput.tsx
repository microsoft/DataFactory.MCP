/**
 * ConnectionNameInput - Text input for connection display name.
 * Class component.
 */

import { Component, ReactNode } from "react";
import { BaseStyles } from "../../../shared";
import { ConnectionNameInputStyles as styles } from "./ConnectionNameInput.styles";
import type { ConnectionNameInputProps } from "./types";

export class ConnectionNameInput extends Component<ConnectionNameInputProps> {
  constructor(props: ConnectionNameInputProps) {
    super(props);
    this.handleChange = this.handleChange.bind(this);
  }

  private handleChange(e: React.ChangeEvent<HTMLInputElement>): void {
    this.props.onChange(e.target.value);
  }

  render(): ReactNode {
    const { value, disabled = false } = this.props;

    return (
      <div style={BaseStyles.formGroup}>
        <label htmlFor="connection-name" style={BaseStyles.label}>
          Connection name <span style={styles.required}>*</span>
        </label>
        <input
          id="connection-name"
          type="text"
          value={value}
          onChange={this.handleChange}
          disabled={disabled}
          placeholder="Enter a display name for this connection"
          style={BaseStyles.input}
        />
      </div>
    );
  }
}
