/**
 * Shared styles for MCP UI Apps.
 * Uses VS Code CSS variables for theme compatibility.
 */
import { CSSProperties } from "react";
export class BaseStyles {
  static body: CSSProperties = {
    fontFamily:
      "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif",
    background: "var(--vscode-editor-background, #1e1e1e)",
    color: "var(--vscode-foreground, #cccccc)",
    padding: "12px",
    lineHeight: 1.4,
    margin: 0,
    boxSizing: "border-box",
  };
  static container: CSSProperties = {
    maxWidth: "100%",
  };
  static h1: CSSProperties = {
    fontSize: "1.05rem",
    fontWeight: 600,
    marginBottom: "12px",
    color: "var(--vscode-foreground, #cccccc)",
  };
  static h2: CSSProperties = {
    fontSize: "1rem",
    fontWeight: 600,
    marginBottom: "8px",
    color: "var(--vscode-foreground, #cccccc)",
  };
  static formGroup: CSSProperties = {
    marginBottom: "10px",
  };
  static label: CSSProperties = {
    display: "block",
    marginBottom: "4px",
    fontWeight: 500,
    fontSize: "0.8rem",
    color: "var(--vscode-foreground, #cccccc)",
  };
  static input: CSSProperties = {
    width: "100%",
    padding: "6px 8px",
    border: "1px solid var(--vscode-input-border, #3c3c3c)",
    borderRadius: "4px",
    background: "var(--vscode-input-background, #3c3c3c)",
    color: "var(--vscode-input-foreground, #cccccc)",
    fontFamily: "inherit",
    fontSize: "0.8rem",
    boxSizing: "border-box",
  };
  static textarea: CSSProperties = {
    width: "100%",
    padding: "6px 8px",
    border: "1px solid var(--vscode-input-border, #3c3c3c)",
    borderRadius: "4px",
    background: "var(--vscode-input-background, #3c3c3c)",
    color: "var(--vscode-input-foreground, #cccccc)",
    fontFamily: "inherit",
    fontSize: "0.8rem",
    resize: "vertical",
    minHeight: "80px",
    boxSizing: "border-box",
  };
  static select: CSSProperties = {
    width: "100%",
    padding: "6px 8px",
    border: "1px solid var(--vscode-input-border, #3c3c3c)",
    borderRadius: "4px",
    background: "var(--vscode-input-background, #3c3c3c)",
    color: "var(--vscode-input-foreground, #cccccc)",
    fontFamily: "inherit",
    fontSize: "0.8rem",
    boxSizing: "border-box",
  };
  static button: CSSProperties = {
    padding: "6px 14px",
    background: "var(--vscode-button-background, #0e639c)",
    color: "var(--vscode-button-foreground, #ffffff)",
    border: "none",
    borderRadius: "4px",
    fontSize: "0.8rem",
    fontWeight: 500,
    cursor: "pointer",
  };
  static buttonHover: CSSProperties = {
    background: "var(--vscode-button-hoverBackground, #1177bb)",
  };
  static buttonDisabled: CSSProperties = {
    opacity: 0.6,
    cursor: "not-allowed",
  };
  static buttonSecondary: CSSProperties = {
    padding: "6px 14px",
    background: "var(--vscode-button-secondaryBackground, #3c3c3c)",
    color: "var(--vscode-button-secondaryForeground, #cccccc)",
    border: "none",
    borderRadius: "4px",
    fontSize: "0.8rem",
    fontWeight: 500,
    cursor: "pointer",
  };
  static statusSuccess: CSSProperties = {
    marginTop: "16px",
    padding: "10px",
    borderRadius: "4px",
    fontSize: "0.875rem",
    background: "var(--vscode-inputValidation-infoBackground, #063b49)",
    border: "1px solid var(--vscode-inputValidation-infoBorder, #007acc)",
  };
  static statusError: CSSProperties = {
    marginTop: "16px",
    padding: "10px",
    borderRadius: "4px",
    fontSize: "0.875rem",
    background: "var(--vscode-inputValidation-errorBackground, #5a1d1d)",
    border: "1px solid var(--vscode-inputValidation-errorBorder, #be1100)",
  };
  static statusInfo: CSSProperties = {
    marginTop: "16px",
    padding: "10px",
    borderRadius: "4px",
    fontSize: "0.875rem",
    background: "var(--vscode-inputValidation-warningBackground, #352a05)",
    border: "1px solid var(--vscode-inputValidation-warningBorder, #b89500)",
  };
}

/**
 * Apply body styles to document.body
 */
export function applyBodyStyles(): void {
  Object.assign(document.body.style, BaseStyles.body);
}
