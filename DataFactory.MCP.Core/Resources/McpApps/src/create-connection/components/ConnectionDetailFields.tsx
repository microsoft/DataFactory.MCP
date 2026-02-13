/**
 * ConnectionDetailFields - Dynamic text fields for connection details.
 * Renders fields based on the selected data source type's labels
 * (e.g., "server" + "database" for SQL, "url" for Web).
 * Class component.
 */

import { Component, ReactNode } from "react";
import { DataSourceLabel } from "../services/types";
import { baseStyles } from "../../shared";

export interface ConnectionDetailFieldsProps {
  labels: DataSourceLabel[];
  values: Record<string, string>;
  onChange: (fieldName: string, value: string) => void;
  disabled?: boolean;
}

export class ConnectionDetailFields extends Component<ConnectionDetailFieldsProps> {
  private handleChange(fieldName: string, value: string): void {
    this.props.onChange(fieldName, value);
  }

  render(): ReactNode {
    const { labels, values, disabled = false } = this.props;

    if (labels.length === 0) {
      return null;
    }

    return (
      <div>
        {labels.map((label) => (
          <div key={label.name} style={baseStyles.formGroup}>
            <label
              htmlFor={`conn-detail-${label.name}`}
              style={baseStyles.label}
            >
              {this.formatLabel(label.name)}
              {label.required && <span style={styles.required}> *</span>}
            </label>
            <input
              id={`conn-detail-${label.name}`}
              type="text"
              value={values[label.name] || ""}
              onChange={(e) => this.handleChange(label.name, e.target.value)}
              disabled={disabled}
              placeholder={`Enter ${label.name}`}
              style={baseStyles.input}
            />
          </div>
        ))}
      </div>
    );
  }

  /** Capitalize first letter of field name for display */
  private formatLabel(name: string): string {
    return name.charAt(0).toUpperCase() + name.slice(1);
  }
}

const styles: Record<string, React.CSSProperties> = {
  required: { color: "var(--vscode-errorForeground, #f48771)" },
};
