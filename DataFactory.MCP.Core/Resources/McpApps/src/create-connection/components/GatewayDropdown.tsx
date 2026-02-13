/**
 * GatewayDropdown - Dropdown for gateway cluster selection.
 * Class component.
 */

import { Component, ReactNode } from "react";
import { Gateway } from "../services/types";
import { baseStyles } from "../../shared";

export interface GatewayDropdownProps {
  gateways: Gateway[];
  selectedGatewayId: string | null;
  onSelect: (gatewayId: string) => void;
  isLoading: boolean;
  disabled?: boolean;
  label?: string;
}

export class GatewayDropdown extends Component<GatewayDropdownProps> {
  constructor(props: GatewayDropdownProps) {
    super(props);
    this.handleChange = this.handleChange.bind(this);
  }

  private handleChange(e: React.ChangeEvent<HTMLSelectElement>): void {
    this.props.onSelect(e.target.value);
  }

  render(): ReactNode {
    const {
      gateways,
      selectedGatewayId,
      isLoading,
      disabled = false,
      label = "Gateway cluster name",
    } = this.props;

    return (
      <div style={baseStyles.formGroup}>
        <label htmlFor="gateway-select" style={baseStyles.label}>
          {label} <span style={styles.required}>*</span>
        </label>
        <select
          id="gateway-select"
          value={selectedGatewayId || ""}
          onChange={this.handleChange}
          disabled={disabled || isLoading}
          style={baseStyles.select}
          aria-busy={isLoading}
        >
          {isLoading ? (
            <option value="">Loading gateways...</option>
          ) : (
            <>
              <option value="">Select a gateway cluster</option>
              {gateways.map((gw) => (
                <option key={gw.id} value={gw.id}>
                  {gw.name}
                </option>
              ))}
            </>
          )}
        </select>
        {!isLoading && gateways.length === 0 && (
          <div style={styles.hint}>No gateways available for this mode.</div>
        )}
      </div>
    );
  }
}

const styles: Record<string, React.CSSProperties> = {
  required: { color: "var(--vscode-errorForeground, #f48771)" },
  hint: {
    marginTop: "4px",
    fontSize: "0.75rem",
    color: "var(--vscode-descriptionForeground, #888)",
  },
};
