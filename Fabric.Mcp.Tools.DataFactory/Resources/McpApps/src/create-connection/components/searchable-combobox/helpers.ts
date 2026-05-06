/**
 * Helper functions for the SearchableComboBox component.
 */

import { ComboBoxOption } from "./types";

/**
 * Filter options by search text (case-insensitive substring match).
 */
export function filterOptions(
  options: ComboBoxOption[],
  searchText: string,
): ComboBoxOption[] {
  if (!searchText) return options;
  const lower = searchText.toLowerCase();
  return options.filter((opt) => opt.label.toLowerCase().includes(lower));
}

/**
 * Get the display label for a selected value.
 * Returns the matching option's label, or the raw value as fallback.
 */
export function getDisplayText(
  selectedValue: string,
  options: ComboBoxOption[],
): string {
  if (!selectedValue) return "";
  const found = options.find((o) => o.value === selectedValue);
  return found ? found.label : selectedValue;
}

/**
 * Clamp a highlighted index to the valid range for the given list length.
 */
export function clampIndex(index: number, length: number): number {
  if (length === 0) return -1;
  return Math.max(0, Math.min(index, length - 1));
}
