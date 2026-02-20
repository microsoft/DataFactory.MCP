/**
 * Styles for the SuccessBanner component.
 */

import { CSSProperties } from "react";

export class SuccessBannerStyles {
  static title: CSSProperties = {
    fontWeight: 600,
    marginBottom: "8px",
    fontSize: "1rem",
  };
  static detail: CSSProperties = {
    fontSize: "0.85rem",
    marginBottom: "4px",
    color: "var(--vscode-foreground, #cccccc)",
  };
}
