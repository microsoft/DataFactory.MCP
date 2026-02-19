/**
 * Styles for the LoadingSpinner component.
 */

export const loadingSpinnerStyles: Record<string, React.CSSProperties> = {
  container: {
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
    justifyContent: "center",
    padding: "40px",
    color: "var(--vscode-foreground, #cccccc)",
  },
  spinner: {
    fontSize: "2rem",
    marginBottom: "12px",
  },
  message: {
    fontSize: "0.9rem",
    opacity: 0.8,
  },
};
