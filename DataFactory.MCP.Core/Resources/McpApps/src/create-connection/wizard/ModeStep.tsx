/**
 * ModeStep - First wizard step: choose connection mode.
 * Selecting a mode automatically advances to the details step.
 */

import { ConnectionMode } from "../services/types";
import { ConnectionModeSelector } from "../components";
import { ModeStepStyles as styles } from "./ModeStep.styles";

interface ModeStepProps {
  connectionMode: ConnectionMode | null;
  onModeChange: (mode: ConnectionMode) => void;
}

export function ModeStep({ connectionMode, onModeChange }: ModeStepProps) {
  return (
    <>
      <p style={styles.subtitle}>Choose a connectivity type to get started.</p>
      <ConnectionModeSelector
        selectedMode={connectionMode}
        onModeChange={onModeChange}
        disabled={false}
      />
    </>
  );
}
