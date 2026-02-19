/**
 * ConnectionTypeDropdown - Searchable dropdown to select a data source type.
 * Populated from the list_supported_cloud_data_sources tool result.
 * Class component.
 */

import { Component, ReactNode } from "react";
import { SupportedDataSourceType } from "../services/types";
import { SearchableComboBox, ComboBoxOption } from "./SearchableComboBox";

export interface ConnectionTypeDropdownProps {
  dataSourceTypes: SupportedDataSourceType[];
  selectedType: string;
  onSelect: (dataSourceType: string) => void;
  isLoading: boolean;
  disabled?: boolean;
}

export class ConnectionTypeDropdown extends Component<ConnectionTypeDropdownProps> {
  render(): ReactNode {
    const {
      dataSourceTypes,
      selectedType,
      isLoading,
      disabled = false,
    } = this.props;

    // Sort alphabetically by display name and map to ComboBoxOption
    const options: ComboBoxOption[] = [...dataSourceTypes]
      .sort((a, b) =>
        (a.displayName || a.dataSourceType).localeCompare(
          b.displayName || b.dataSourceType,
        ),
      )
      .map((ds) => ({
        value: ds.displayName || ds.dataSourceType,
        label: ds.displayName || ds.dataSourceType,
      }));

    return (
      <SearchableComboBox
        id="connection-type"
        label="Connection type"
        required
        options={options}
        selectedValue={selectedType}
        onSelect={this.props.onSelect}
        isLoading={isLoading}
        disabled={disabled}
        placeholder="Search data source types..."
        loadingText="Loading data source types..."
      />
    );
  }
}
