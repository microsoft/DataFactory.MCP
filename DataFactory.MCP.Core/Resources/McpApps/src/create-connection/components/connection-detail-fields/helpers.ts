/**
 * Helper functions for the ConnectionDetailFields component.
 */

/**
 * Capitalize the first letter of a field name for display.
 */
export function formatLabel(name: string): string {
  return name.charAt(0).toUpperCase() + name.slice(1);
}
