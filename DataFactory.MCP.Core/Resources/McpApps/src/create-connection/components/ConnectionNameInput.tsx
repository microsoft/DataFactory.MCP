/**
 * ConnectionNameInput - Text input for connection display name.
 * Class component.
 */

import { Component, ReactNode } from "react";
import { baseStyles } from "../../shared";

export interface ConnectionNameInputProps {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}

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
      <div style={baseStyles.formGroup}>
        <label htmlFor="connection-name" style={baseStyles.label}>
          Connection name <span style={styles.required}>*</span>
        </label>
        <input
          id="connection-name"
          type="text"
          value={value}
          onChange={this.handleChange}
          disabled={disabled}
          placeholder="Enter a display name for this connection"
          style={baseStyles.input}
        />
      </div>
    );
  }
}

const styles: Record<string, React.CSSProperties> = {
  required: { color: "var(--vscode-errorForeground, #f48771)" },
};
