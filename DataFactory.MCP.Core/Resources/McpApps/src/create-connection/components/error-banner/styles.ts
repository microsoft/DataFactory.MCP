/**
 * Styles for the ErrorBanner component.
 */

export const errorBannerStyles: Record<string, React.CSSProperties> = {
  message: {
    fontSize: "0.85rem",
    marginBottom: "8px",
  },
  retryButton: {
    padding: "4px 12px",
    background: "var(--vscode-button-background, #0e639c)",
    color: "var(--vscode-button-foreground, #ffffff)",
    border: "none",
    borderRadius: "4px",
    fontSize: "0.8rem",
    cursor: "pointer",
  },
};
