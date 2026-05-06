import { CSSProperties } from "react";

export class WizardStepIndicatorStyles {
  static container: CSSProperties = {
    display: "flex",
    alignItems: "center",
    marginBottom: "12px",
    fontSize: "0.75rem",
  };

  static stepWrapper: CSSProperties = {
    display: "flex",
    alignItems: "center",
  };

  static divider: CSSProperties = {
    color: "var(--vscode-descriptionForeground, #888)",
    margin: "0 3px",
    fontSize: "0.7rem",
  };

  static stepButton(isCurrent: boolean, isPast: boolean): CSSProperties {
    return {
      background: "none",
      border: "none",
      padding: "2px 4px",
      cursor: isPast ? "pointer" : "default",
      color: isCurrent
        ? "var(--vscode-foreground, #ccc)"
        : isPast
          ? "var(--vscode-textLink-foreground, #3794ff)"
          : "var(--vscode-descriptionForeground, #888)",
      fontWeight: isCurrent ? 600 : 400,
      fontSize: "0.75rem",
      textDecoration: isPast ? "underline" : "none",
    };
  }
}
