/**
 * ConnectionModeSelector - Tab buttons for connection mode selection.
 * Class component.
 */

import { Component, ReactNode } from "react";
import {
  ConnectionMode,
  connectionModeLabels,
  connectionModeIcons,
} from "../services/types";

export interface ConnectionModeSelectorProps {
  selectedMode: ConnectionMode;
  onModeChange: (mode: ConnectionMode) => void;
  disabled?: boolean;
}

const modes: ConnectionMode[] = [
  "OnPremises",
  "VirtualNetwork",
  "StreamingVirtualNetwork",
  "Cloud",
];

export class ConnectionModeSelector extends Component<ConnectionModeSelectorProps> {
  render(): ReactNode {
    const { selectedMode, onModeChange, disabled = false } = this.props;

    return (
      <div style={styles.container}>
        {modes.map((mode) => (
          <button
            key={mode}
            type="button"
            onClick={() => !disabled && onModeChange(mode)}
            disabled={disabled}
            style={{
              ...styles.tab,
              ...(selectedMode === mode ? styles.tabSelected : {}),
              ...(disabled ? styles.tabDisabled : {}),
            }}
            aria-pressed={selectedMode === mode}
          >
            <span style={styles.icon}>{connectionModeIcons[mode]}</span>
            <span style={styles.label}>{connectionModeLabels[mode]}</span>
          </button>
        ))}
      </div>
    );
  }
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    display: "flex",
    gap: "8px",
    marginBottom: "20px",
    flexWrap: "wrap",
  },
  tab: {
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
    padding: "12px 16px",
    minWidth: "90px",
    border: "1px solid var(--vscode-input-border, #3c3c3c)",
    borderRadius: "4px",
    background: "var(--vscode-input-background, #3c3c3c)",
    color: "var(--vscode-foreground, #cccccc)",
    cursor: "pointer",
    fontSize: "0.75rem",
    transition: "all 0.15s ease",
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
  icon: {
    fontSize: "1.25rem",
    marginBottom: "4px",
  },
  label: {
    textAlign: "center" as const,
    lineHeight: 1.2,
  },
};
