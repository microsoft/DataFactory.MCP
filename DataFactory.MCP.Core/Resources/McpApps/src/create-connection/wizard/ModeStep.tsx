/**
 * ModeStep - First wizard step: choose connection mode.
 * Selecting a mode automatically advances to the details step.
 */

import { ConnectionMode } from "../services/types";
import { ConnectionModeSelector } from "../components";

interface ModeStepProps {
  connectionMode: ConnectionMode;
  onModeChange: (mode: ConnectionMode) => void;
}

export function ModeStep({ connectionMode, onModeChange }: ModeStepProps) {
  return (
    <>
      <p
        style={{
          fontSize: "0.8rem",
          color: "var(--vscode-descriptionForeground, #888)",
          marginBottom: "10px",
          marginTop: 0,
        }}
      >
        Choose a connectivity type to get started.
      </p>
      <ConnectionModeSelector
        selectedMode={connectionMode}
        onModeChange={onModeChange}
        disabled={false}
      />
    </>
  );
}
