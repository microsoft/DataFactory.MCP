/**
 * Styles for the SearchableComboBox component.
 * Uses VS Code CSS variables for theme compatibility.
 */

import { CSSProperties } from "react";

export class ComboBoxStyles {
  static required: CSSProperties = {
    color: "var(--vscode-errorForeground, #f48771)",
  };
  static container: CSSProperties = {
    position: "relative",
  };
  static inputWrapper: CSSProperties = {
    position: "relative",
    display: "flex",
    alignItems: "center",
  };
  static clearButton: CSSProperties = {
    position: "absolute",
    right: "8px",
    background: "none",
    border: "none",
    color: "var(--vscode-input-foreground, #cccccc)",
    cursor: "pointer",
    fontSize: "0.75rem",
    padding: "4px",
    opacity: 0.7,
    lineHeight: 1,
  };
  static dropdown: CSSProperties = {
    position: "absolute",
    top: "100%",
    left: 0,
    right: 0,
    maxHeight: "160px",
    overflowY: "auto",
    background: "var(--vscode-dropdown-background, #3c3c3c)",
    border: "1px solid var(--vscode-dropdown-border, #3c3c3c)",
    borderRadius: "0 0 4px 4px",
    margin: 0,
    padding: 0,
    listStyle: "none",
    zIndex: 1000,
    boxShadow: "0 4px 8px rgba(0,0,0,0.3)",
  };
  static option: CSSProperties = {
    display: "block",
    width: "100%",
    padding: "5px 8px",
    cursor: "pointer",
    fontSize: "0.8rem",
    color: "var(--vscode-dropdown-foreground, #cccccc)",
    background: "none",
    border: "none",
    textAlign: "left",
    fontFamily: "inherit",
    whiteSpace: "nowrap",
    overflow: "hidden",
    textOverflow: "ellipsis",
    boxSizing: "border-box",
  };
  static optionHighlighted: CSSProperties = {
    background: "var(--vscode-list-activeSelectionBackground, #094771)",
    color: "var(--vscode-list-activeSelectionForeground, #ffffff)",
  };
  static noResults: CSSProperties = {
    padding: "5px 8px",
    fontSize: "0.8rem",
    color: "var(--vscode-descriptionForeground, #888)",
    fontStyle: "italic",
  };
}
