/**
 * Styles for the ConnectionModeSelector component.
 */

export const connectionModeSelectorStyles: Record<string, React.CSSProperties> =
  {
    container: {
      display: "flex",
      gap: "4px",
      marginBottom: "14px",
      flexWrap: "wrap",
    },
    tab: {
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
      whiteSpace: "nowrap" as const,
    },
    tabSelected: {
      borderColor: "var(--vscode-focusBorder, #007acc)",
      background: "var(--vscode-list-activeSelectionBackground, #094771)",
      color: "var(--vscode-list-activeSelectionForeground, #ffffff)",
    },
    tabDisabled: {
      opacity: 0.5,
      cursor: "not-allowed",
    },
  };
