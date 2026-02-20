/**
 * Types for the SearchableComboBox component.
 */

export interface ComboBoxOption {
  value: string;
  label: string;
}

export interface SearchableComboBoxProps {
  id: string;
  options: ComboBoxOption[];
  selectedValue: string;
  onSelect: (value: string) => void;
  placeholder?: string;
  loadingText?: string;
  isLoading?: boolean;
  disabled?: boolean;
  label: string;
  required?: boolean;
}

export interface SearchableComboBoxState {
  searchText: string;
  isOpen: boolean;
  highlightedIndex: number;
}
