/**
 * GatewayDropdown - Searchable dropdown for gateway cluster selection.
 * Class component.
 */

import { Component, ReactNode } from "react";
import { SearchableComboBox, ComboBoxOption } from "../searchable-combobox";
import { gatewayDropdownStyles as styles } from "./styles";
import type { GatewayDropdownProps } from "./types";

export class GatewayDropdown extends Component<GatewayDropdownProps> {
  render(): ReactNode {
    const {
      gateways,
      selectedGatewayId,
      isLoading,
      disabled = false,
      label = "Gateway cluster name",
    } = this.props;

    const options: ComboBoxOption[] = gateways.map((gw) => ({
      value: gw.id,
      label: gw.name,
    }));

    return (
      <div>
        <SearchableComboBox
          id="gateway-select"
          label={label}
          required
          options={options}
          selectedValue={selectedGatewayId || ""}
          onSelect={this.props.onSelect}
          isLoading={isLoading}
          disabled={disabled}
          placeholder="Search gateways..."
          loadingText="Loading gateways..."
        />
        {!isLoading && gateways.length === 0 && (
          <div style={styles.hint}>No gateways available for this mode.</div>
        )}
      </div>
    );
  }
}
