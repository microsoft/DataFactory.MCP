/**
 * Styles for the LoadingSpinner component.
 */

import { CSSProperties } from "react";

export class LoadingSpinnerStyles {
  static container: CSSProperties = {
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
    justifyContent: "center",
    padding: "40px",
    color: "var(--vscode-foreground, #cccccc)",
  };
  static spinner: CSSProperties = {
    fontSize: "2rem",
    marginBottom: "12px",
  };
  static message: CSSProperties = {
    fontSize: "0.9rem",
    opacity: 0.8,
  };
}
