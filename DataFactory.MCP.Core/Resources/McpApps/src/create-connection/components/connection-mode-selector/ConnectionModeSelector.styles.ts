/**
 * Styles for the ConnectionModeSelector component.
 */

import { CSSProperties } from "react";

export class ConnectionModeSelectorStyles {
  static container: CSSProperties = {
    display: "flex",
    gap: "4px",
    marginBottom: "14px",
    flexWrap: "wrap",
  };
  static tab: CSSProperties = {
    display: "flex",
    flexDirection: "row",
    alignItems: "center",
    gap: "5px",
    padding: "5px 10px",
    minWidth: 0,
    border: "1px solid var(--vscode-input-border, #3c3c3c)",
    borderRadius: "4px",
    background: "var(--vscode-input-background, #3c3c3c)",
    color: "var(--vscode-foreground, #cccccc)",
    cursor: "pointer",
    fontSize: "0.75rem",
    transition: "all 0.15s ease",
    whiteSpace: "nowrap",
  };
  static tabSelected: CSSProperties = {
    borderColor: "var(--vscode-focusBorder, #007acc)",
    background: "var(--vscode-list-activeSelectionBackground, #094771)",
    color: "var(--vscode-list-activeSelectionForeground, #ffffff)",
  };
  static tabDisabled: CSSProperties = {
    opacity: 0.5,
    cursor: "not-allowed",
  };
}
