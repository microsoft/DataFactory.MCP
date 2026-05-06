/**
 * Types for the ConnectionTypeDropdown component.
 */

import { SupportedDataSourceType } from "../../services/types";

export interface ConnectionTypeDropdownProps {
  dataSourceTypes: SupportedDataSourceType[];
  selectedType: string;
  onSelect: (dataSourceType: string) => void;
  isLoading: boolean;
  disabled?: boolean;
}
