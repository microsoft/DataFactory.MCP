/**
 * Styles for the CredentialSection component.
 */

import { CSSProperties } from "react";

export class CredentialSectionStyles {
  static section: CSSProperties = {
    marginTop: "10px",
    padding: "10px",
    border: "1px solid var(--vscode-input-border, #3c3c3c)",
    borderRadius: "4px",
  };
  static sectionHeader: CSSProperties = {
    fontSize: "0.8rem",
    fontWeight: 600,
    marginBottom: "8px",
    color: "var(--vscode-foreground, #cccccc)",
  };
  static required: CSSProperties = {
    color: "var(--vscode-errorForeground, #f48771)",
  };
  static checkboxGroup: CSSProperties = { marginTop: "8px" };
  static checkboxLabel: CSSProperties = {
    display: "flex",
    alignItems: "center",
    gap: "6px",
    cursor: "pointer",
    fontSize: "0.8rem",
    color: "var(--vscode-foreground, #cccccc)",
  };
  static checkbox: CSSProperties = {
    accentColor: "var(--vscode-focusBorder, #007acc)",
  };
}
