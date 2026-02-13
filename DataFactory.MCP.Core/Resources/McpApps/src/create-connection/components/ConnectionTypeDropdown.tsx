/**
 * ConnectionTypeDropdown - Dropdown to select a data source type.
 * Populated from the list_supported_cloud_data_sources tool result.
 * Class component.
 */

import { Component, ReactNode } from "react";
import { SupportedDataSourceType } from "../services/types";
import { baseStyles } from "../../shared";

export interface ConnectionTypeDropdownProps {
  dataSourceTypes: SupportedDataSourceType[];
  selectedType: string;
  onSelect: (dataSourceType: string) => void;
  isLoading: boolean;
  disabled?: boolean;
}

export class ConnectionTypeDropdown extends Component<ConnectionTypeDropdownProps> {
  constructor(props: ConnectionTypeDropdownProps) {
    super(props);
    this.handleChange = this.handleChange.bind(this);
  }

  private handleChange(e: React.ChangeEvent<HTMLSelectElement>): void {
    this.props.onSelect(e.target.value);
  }

  render(): ReactNode {
    const {
      dataSourceTypes,
      selectedType,
      isLoading,
      disabled = false,
    } = this.props;

    // Sort alphabetically by display name
    const sorted = [...dataSourceTypes].sort((a, b) =>
      (a.displayName || a.dataSourceType).localeCompare(
        b.displayName || b.dataSourceType,
      ),
    );

    return (
      <div style={baseStyles.formGroup}>
        <label htmlFor="connection-type" style={baseStyles.label}>
          Connection type <span style={styles.required}>*</span>
        </label>
        <select
          id="connection-type"
          value={selectedType}
          onChange={this.handleChange}
          disabled={disabled || isLoading}
          style={baseStyles.select}
          aria-busy={isLoading}
        >
          {isLoading ? (
            <option value="">Loading data source types...</option>
          ) : (
            <>
              <option value="">Select a data source type</option>
              {sorted.map((ds) => (
                <option
                  key={ds.displayName || ds.dataSourceType}
                  value={ds.displayName || ds.dataSourceType}
                >
                  {ds.displayName || ds.dataSourceType}
                </option>
              ))}
            </>
          )}
        </select>
      </div>
    );
  }
}

const styles: Record<string, React.CSSProperties> = {
  required: { color: "var(--vscode-errorForeground, #f48771)" },
};
