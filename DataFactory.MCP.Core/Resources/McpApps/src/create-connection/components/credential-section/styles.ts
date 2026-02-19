/**
 * Styles for the CredentialSection component.
 */

export const credentialSectionStyles: Record<string, React.CSSProperties> = {
  section: {
    marginTop: "10px",
    padding: "10px",
    border: "1px solid var(--vscode-input-border, #3c3c3c)",
    borderRadius: "4px",
  },
  sectionHeader: {
    fontSize: "0.8rem",
    fontWeight: 600,
    marginBottom: "8px",
    color: "var(--vscode-foreground, #cccccc)",
  },
  required: { color: "var(--vscode-errorForeground, #f48771)" },
  checkboxGroup: {
    marginTop: "8px",
  },
  checkboxLabel: {
    display: "flex",
    alignItems: "center",
    gap: "6px",
    cursor: "pointer",
    fontSize: "0.8rem",
    color: "var(--vscode-foreground, #cccccc)",
  },
  checkbox: {
    accentColor: "var(--vscode-focusBorder, #007acc)",
  },
};
