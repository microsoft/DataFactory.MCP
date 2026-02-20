/**
 * Types for the ConnectionModeSelector component.
 */

import { ConnectionMode } from "../../services/types";

export interface ConnectionModeSelectorProps {
  selectedMode: ConnectionMode | null;
  onModeChange: (mode: ConnectionMode) => void;
  disabled?: boolean;
}

export const modes: ConnectionMode[] = [
  "OnPremises",
  "VirtualNetwork",
  "StreamingVirtualNetwork",
  "Cloud",
];
