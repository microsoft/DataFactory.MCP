/**
 * Styles for the CredentialSection component.
 */

export const credentialSectionStyles: Record<string, React.CSSProperties> = {
  section: {
    marginTop: "16px",
    padding: "16px",
    border: "1px solid var(--vscode-input-border, #3c3c3c)",
    borderRadius: "4px",
  },
  sectionHeader: {
    fontSize: "0.9rem",
    fontWeight: 600,
    marginBottom: "12px",
    color: "var(--vscode-foreground, #cccccc)",
  },
  required: { color: "var(--vscode-errorForeground, #f48771)" },
  checkboxGroup: {
    marginTop: "12px",
  },
  checkboxLabel: {
    display: "flex",
    alignItems: "center",
    gap: "8px",
    cursor: "pointer",
    fontSize: "0.875rem",
    color: "var(--vscode-foreground, #cccccc)",
  },
  checkbox: {
    accentColor: "var(--vscode-focusBorder, #007acc)",
  },
};
