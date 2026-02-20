/**
 * ConnectionModeSelector - Tab buttons for connection mode selection.
 * Class component.
 */

import { Component, ReactNode } from "react";
import { connectionModeLabels } from "../../services/types";
import { ConnectionModeSelectorStyles as styles } from "./ConnectionModeSelector.styles";
import type { ConnectionModeSelectorProps } from "./types";
import { modes } from "./types";

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
            {connectionModeLabels[mode]}
          </button>
        ))}
      </div>
    );
  }
}
