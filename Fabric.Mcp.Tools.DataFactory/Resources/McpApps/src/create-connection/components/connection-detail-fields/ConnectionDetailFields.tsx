/**
 * ConnectionDetailFields - Dynamic text fields for connection details.
 * Renders fields based on the selected data source type's labels
 * (e.g., "server" + "database" for SQL, "url" for Web).
 * Class component.
 */

import { Component, ReactNode } from "react";
import { BaseStyles } from "../../../shared";
import { ConnectionDetailFieldsStyles as styles } from "./ConnectionDetailFields.styles";
import { formatLabel } from "./helpers";
import type { ConnectionDetailFieldsProps } from "./types";

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
          <div key={label.name} style={BaseStyles.formGroup}>
            <label
              htmlFor={`conn-detail-${label.name}`}
              style={BaseStyles.label}
            >
              {formatLabel(label.name)}
              {label.required && <span style={styles.required}> *</span>}
            </label>
            <input
              id={`conn-detail-${label.name}`}
              type="text"
              value={values[label.name] || ""}
              onChange={(e) => this.handleChange(label.name, e.target.value)}
              disabled={disabled}
              placeholder={`Enter ${label.name}`}
              style={BaseStyles.input}
            />
          </div>
        ))}
      </div>
    );
  }
}
